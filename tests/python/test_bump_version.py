import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


def _load_module():
    root = Path(__file__).resolve().parents[2]
    script_path = root / "bump-version.py"
    spec = importlib.util.spec_from_file_location("bump_version_script", script_path)
    module = importlib.util.module_from_spec(spec)
    assert spec is not None and spec.loader is not None
    spec.loader.exec_module(module)
    return module


class BumpVersionScriptTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.mod = _load_module()

    def test_parse_version_parses_semver_and_v_prefix(self):
        self.assertEqual((1, 2, 3), self.mod.parse_version("1.2.3"))
        self.assertEqual((2, 0, 9), self.mod.parse_version("v2.0.9"))

    def test_parse_version_rejects_invalid_format(self):
        with self.assertRaises(ValueError):
            self.mod.parse_version("1.2")
        with self.assertRaises(ValueError):
            self.mod.parse_version("1.2.3.4")

    def test_bump_version_major_minor_patch(self):
        self.assertEqual("2.0.0", self.mod.bump_version("1.9.9", "major"))
        self.assertEqual("1.10.0", self.mod.bump_version("1.9.9", "minor"))
        self.assertEqual("1.9.10", self.mod.bump_version("1.9.9", "patch"))

    def test_bump_version_rejects_unknown_bump_type(self):
        with self.assertRaises(ValueError):
            self.mod.bump_version("1.2.3", "invalid")

    def test_update_version_json_and_read_canonical_version_round_trip(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            shared = root / "versions" / "shared"
            shared.mkdir(parents=True, exist_ok=True)
            version_file = shared / "version.json"
            version_file.write_text(json.dumps({"major": 0, "minor": 1, "patch": 0}))

            self.mod.update_version_json(root, "3.4.5")
            self.assertEqual("3.4.5", self.mod.read_canonical_version(root))

            data = json.loads(version_file.read_text())
            self.assertEqual(3, data["major"])
            self.assertEqual(4, data["minor"])
            self.assertEqual(5, data["patch"])


if __name__ == "__main__":
    unittest.main()
