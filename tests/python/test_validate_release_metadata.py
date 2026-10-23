import importlib.util
import tempfile
import unittest
from pathlib import Path


def _load_module():
    root = Path(__file__).resolve().parents[2]
    path = root / "tools" / "validate_release_metadata.py"
    spec = importlib.util.spec_from_file_location("validate_release_metadata", path)
    module = importlib.util.module_from_spec(spec)
    assert spec is not None and spec.loader is not None
    spec.loader.exec_module(module)
    return module


class ValidateReleaseMetadataTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.mod = _load_module()
        cls.root = Path(__file__).resolve().parents[2]

    def test_repository_metadata_is_consistent(self):
        self.assertEqual([], self.mod.validate(self.root))

    def test_validator_reports_version_drift(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "versions" / "shared").mkdir(parents=True)
            (root / "versions" / "shared" / "version.json").write_text(
                '{"major": 1, "minor": 2, "patch": 3}'
            )
            (root / "project.godot").write_text(
                'config/version="1.2.0"\n'
                "config/version_major=1\n"
                "config/version_minor=2\n"
                "config/version_patch=0\n"
            )
            errors = self.mod.validate(root)
            self.assertTrue(any("project.godot: found '1.2.0'" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
