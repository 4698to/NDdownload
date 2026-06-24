import json
import tempfile
from pathlib import Path

from installbox_pack import (
    compute_sha256_base64,
    pack_item,
    publish,
    refresh_items_changes,
    scan_parent_directory,
)


def test_installbox_pack_flow():
    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        pack_dir = root / "pack"
        data_dir = root / "data"
        pack_dir.mkdir()
        data_dir.mkdir()

        src = root / "MyTool"
        src.mkdir()
        (src / "hello.txt").write_text("hello", encoding="utf-8")

        parent = root / "ParentGroup"
        parent.mkdir()
        sub1 = parent / "SubA"
        sub1.mkdir()
        (sub1 / "a.txt").write_text("a", encoding="utf-8")

        scan = scan_parent_directory(str(parent))
        assert scan["parent"]["zipname"] == "ParentGroup"
        assert len(scan["children"]) == 1
        assert scan["children"][0]["zipname"] == "ParentGroupSubA.zip"

        items = [{
            "zipname": "MyTool",
            "targetpath": str(src),
            "savepath": None,
            "type": 0,
            "dirpath": "scripts",
            "SeriesMin": 2015,
            "SeriesMax": 2025,
            "isParent": False,
            "version": 1.0,
            "LastPackTime": "/Date(-62135596800000)/",
            "selected": True,
            "quick": False,
        }]
        stats = refresh_items_changes(items)
        assert stats["changedCount"] >= 1
        assert items[0]["ischange"] is True

        pack_item(items[0], pack_dir, True, [])
        zip_path = pack_dir / "MyTool.zip"
        assert zip_path.is_file()
        assert items[0]["sha"] == compute_sha256_base64(zip_path)
        assert items[0]["version"] == 1.1

        meta = {"Version": "1.0", "_version": "1.0", "remoteUrl": "http://test/"}
        result = publish(meta, items, pack_dir, data_dir, True)
        assert result["version"] == "1.01"
        assert (pack_dir / "InstallBox_version_full.json").is_file()
        assert (pack_dir / "InstallBox_version_full.xml").is_file()
        assert (pack_dir / "updateBox.txt").read_text(encoding="utf-8") == "1.01"
        assert (data_dir / "updateBox.txt").is_file()
        pub = json.loads((data_dir / "InstallBox_version_full.json").read_text(encoding="utf-8"))
        assert "targetpath" not in pub["item"][0]
        xml = (data_dir / "InstallBox_version_full.xml").read_text(encoding="utf-8")
        assert "MaxRoot" in xml


if __name__ == "__main__":
    test_installbox_pack_flow()
    print("all backend tests passed")
