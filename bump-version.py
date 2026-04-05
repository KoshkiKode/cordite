#!/usr/bin/env python3
"""
Semantic versioning automation for multi-platform game exports.
Updates version across project.godot, export_presets.cfg, package manifests, etc.
"""

import sys
import re
import argparse
from pathlib import Path
from typing import Tuple

def parse_version(version_str: str) -> Tuple[int, int, int]:
    """Parse semantic version string (e.g., '0.1.0') into (major, minor, patch)."""
    parts = version_str.strip('v').split('.')
    if len(parts) != 3:
        raise ValueError(f"Invalid version format: {version_str}. Expected X.Y.Z")
    return tuple(int(p) for p in parts)

def format_version(major: int, minor: int, patch: int) -> str:
    """Format version tuple as X.Y.Z string."""
    return f"{major}.{minor}.{patch}"

def bump_version(current: str, bump_type: str) -> str:
    """Bump version by type: 'major', 'minor', or 'patch'."""
    major, minor, patch = parse_version(current)
    
    if bump_type == "major":
        major += 1
        minor = 0
        patch = 0
    elif bump_type == "minor":
        minor += 1
        patch = 0
    elif bump_type == "patch":
        patch += 1
    else:
        raise ValueError(f"Invalid bump type: {bump_type}")
    
    return format_version(major, minor, patch)

def update_project_godot(project_root: Path, new_version: str) -> None:
    """Update version in project.godot."""
    godot_file = project_root / "project.godot"
    if not godot_file.exists():
        print(f"⚠ {godot_file} not found, skipping")
        return
    
    content = godot_file.read_text()
    # Update application/version = "X.Y.Z"
    updated = re.sub(
        r'(application/version\s*=\s*)"[^"]*"',
        f'\\1"{new_version}"',
        content
    )
    
    if updated != content:
        godot_file.write_text(updated)
        print(f"✓ Updated project.godot → {new_version}")
    else:
        print(f"⚠ No version found in project.godot")

def update_export_presets(project_root: Path, new_version: str) -> None:
    """Update version in export_presets.cfg for all platforms."""
    presets_files = list(project_root.glob("**/export_presets.cfg"))
    
    for presets_file in presets_files:
        content = presets_file.read_text()
        
        # Windows: application/product_version
        updated = re.sub(
            r'(application/product_version\s*=\s*)"[^"]*"',
            f'\\1"{new_version}.0"',
            content
        )
        
        # Android: package/version (integer)
        # For Android, use major*100 + minor*10 + patch (e.g., 0.1.0 → 10)
        major, minor, patch = parse_version(new_version)
        android_version = major * 100 + minor * 10 + patch
        updated = re.sub(
            r'(package/version\s*=\s*)(\d+)',
            f'\\1{android_version}',
            updated
        )
        
        # iOS: application/short_version, application/version
        updated = re.sub(
            r'(application/short_version\s*=\s*)"[^"]*"',
            f'\\1"{new_version}"',
            updated
        )
        updated = re.sub(
            r'(application/version\s*=\s*)"[^"]*"',
            f'\\1"{android_version}"',  # Use same integer version for consistency
            updated
        )
        
        if updated != content:
            presets_file.write_text(updated)
            print(f"✓ Updated {presets_file.relative_to(project_root)} → {new_version}")

def update_snapcraft_yaml(project_root: Path, new_version: str) -> None:
    """Update version in snapcraft.yaml."""
    snap_file = project_root / "versions" / "linux" / "snapcraft.yaml"
    if not snap_file.exists():
        print(f"⚠ {snap_file} not found, skipping")
        return
    
    content = snap_file.read_text()
    updated = re.sub(
        r'(version:\s*)[\'"]?[0-9.]+[\'"]?',
        f'\\1{new_version}',
        content
    )
    
    if updated != content:
        snap_file.write_text(updated)
        print(f"✓ Updated snapcraft.yaml → {new_version}")

def update_plist(project_root: Path, new_version: str) -> None:
    """Update version in macOS Info.plist."""
    plist_file = project_root / "versions" / "macos" / "Info.plist"
    if not plist_file.exists():
        print(f"⚠ {plist_file} not found, skipping")
        return
    
    content = plist_file.read_text()
    
    # CFBundleShortVersionString
    updated = re.sub(
        r'(<key>CFBundleShortVersionString</key>\s*<string>)[^<]*(</string>)',
        f'\\1{new_version}\\2',
        content
    )
    
    # CFBundleVersion (integer)
    major, minor, patch = parse_version(new_version)
    bundle_version = major * 100 + minor * 10 + patch
    updated = re.sub(
        r'(<key>CFBundleVersion</key>\s*<string>)[^<]*(</string>)',
        f'\\1{bundle_version}\\2',
        updated
    )
    
    if updated != content:
        plist_file.write_text(updated)
        print(f"✓ Updated Info.plist → {new_version}")

def main():
    parser = argparse.ArgumentParser(
        description="Bump semantic version across all platform export files"
    )
    parser.add_argument(
        "action",
        choices=["major", "minor", "patch", "set"],
        help="Version bump type or 'set' to specify exact version"
    )
    parser.add_argument(
        "version",
        nargs="?",
        help="Exact version for 'set' action (e.g., 1.0.0)"
    )
    parser.add_argument(
        "--project-root",
        type=Path,
        default=Path.cwd(),
        help="Project root directory (default: cwd)"
    )
    
    args = parser.parse_args()
    project_root = args.project_root.resolve()
    
    # Read current version from project.godot
    godot_file = project_root / "project.godot"
    if not godot_file.exists():
        print(f"✗ project.godot not found in {project_root}")
        sys.exit(1)
    
    content = godot_file.read_text()
    match = re.search(r'application/version\s*=\s*"([^"]*)"', content)
    if not match:
        print("✗ Could not find version in project.godot")
        sys.exit(1)
    
    current_version = match.group(1)
    print(f"Current version: {current_version}")
    
    # Determine new version
    if args.action == "set":
        if not args.version:
            print("✗ --version required for 'set' action")
            sys.exit(1)
        new_version = args.version
    else:
        new_version = bump_version(current_version, args.action)
    
    print(f"New version: {new_version}\n")
    
    # Update all files
    update_project_godot(project_root, new_version)
    update_export_presets(project_root, new_version)
    update_snapcraft_yaml(project_root, new_version)
    update_plist(project_root, new_version)
    
    print(f"\n✓ Version bumped to {new_version}")

if __name__ == "__main__":
    main()
