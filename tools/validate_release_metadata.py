#!/usr/bin/env python3
"""Validate that every shipped platform uses the canonical app version."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path


SEMVER = r"\d+\.\d+\.\d+"


def read_version(root: Path) -> tuple[str, int]:
    path = root / "versions" / "shared" / "version.json"
    data = json.loads(path.read_text())
    major, minor, patch = (int(data[key]) for key in ("major", "minor", "patch"))
    version = f"{major}.{minor}.{patch}"
    build_code = major * 10000 + minor * 100 + patch
    return version, build_code


def extract(text: str, pattern: str, label: str, errors: list[str]) -> str | None:
    match = re.search(pattern, text)
    if not match:
        errors.append(f"{label}: expected metadata was not found")
        return None
    return match.group(1)


def validate(root: Path) -> list[str]:
    errors: list[str] = []
    version, build_code = read_version(root)
    version4 = f"{version}.0"

    checks = [
        ("project.godot", r'config/version="(' + SEMVER + r')"', version),
        ("project.godot", r"config/version_major=(\d+)", version.split(".")[0]),
        ("project.godot", r"config/version_minor=(\d+)", version.split(".")[1]),
        ("project.godot", r"config/version_patch=(\d+)", version.split(".")[2]),
        ("export_presets.cfg", r'application/short_version="(' + SEMVER + r')"', version),
        ("versions/shared/export_presets.cfg", r'application/file_version="(' + SEMVER + r'\.0)"', version4),
        ("versions/shared/export_presets.cfg", r'application/product_version="(' + SEMVER + r'\.0)"', version4),
        ("versions/shared/export_presets.cfg", r'application/version="(\d+)"', str(build_code)),
        ("versions/android/AndroidManifest.xml", r'android:versionCode="(\d+)"', str(build_code)),
        ("versions/android/AndroidManifest.xml", r'android:versionName="(' + SEMVER + r')"', version),
        ("versions/android/build.gradle", r"versionCode\s+(\d+)", str(build_code)),
        ("versions/android/build.gradle", r'versionName\s+"(' + SEMVER + r')"', version),
        ("versions/linux/snapcraft.yaml", r"version:\s*(" + SEMVER + r")", version),
        ("versions/linux/debian/control", r"Version:\s*(" + SEMVER + r")", version),
        ("versions/macos/Info.plist", r"<key>CFBundleShortVersionString</key>\s*<string>(" + SEMVER + r")", version),
        ("versions/macos/Info.plist", r"<key>CFBundleVersion</key>\s*<string>(\d+)", str(build_code)),
        ("versions/ios/ios-info.plist", r"<key>CFBundleShortVersionString</key>\s*<string>(" + SEMVER + r")", version),
        ("versions/ios/ios-info.plist", r"<key>CFBundleVersion</key>\s*<string>(\d+)", str(build_code)),
        ("versions/windows/AppxManifest.xml", r'Version="(' + SEMVER + r'\.0)"', version4),
        ("versions/windows/inno-setup.iss", r'#define MyAppVersion "(' + SEMVER + r')"', version),
    ]

    for relative, pattern, expected in checks:
        path = root / relative
        if not path.exists():
            errors.append(f"{relative}: file is missing")
            continue
        actual = extract(path.read_text(), pattern, relative, errors)
        if actual is not None and actual != expected:
            errors.append(f"{relative}: found {actual!r}, expected {expected!r}")

    required_presets = {"Windows Desktop", "Linux/X11", "macOS", "Android", "Android AAB", "iOS"}
    for relative in ("export_presets.cfg", "versions/shared/export_presets.cfg"):
        path = root / relative
        if not path.exists():
            errors.append(f"{relative}: file is missing")
            continue
        names = set(re.findall(r'^name="([^"]+)"$', path.read_text(), re.MULTILINE))
        missing = sorted(required_presets - names)
        if missing:
            errors.append(f"{relative}: missing export presets: {', '.join(missing)}")

    return errors


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    try:
        errors = validate(root)
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        print(f"Release metadata validation failed: {exc}", file=sys.stderr)
        return 1
    if errors:
        print("Release metadata validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1
    version, build_code = read_version(root)
    print(f"Release metadata is consistent: version {version}, build {build_code}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
