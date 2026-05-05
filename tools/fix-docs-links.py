"""
Convert absolute path links in Starlight markdown content to relative .md links.
Astro/Starlight only base-prefixes links when they point at .md/.mdx files in the
content collection. Bare absolute paths like /guides/foo/ are left as-is and 404
on a project Pages site.

Run from repo root:
    python tools/fix-docs-links.py
"""
from pathlib import Path
import os
import re

ROOT = Path("docs/src/content/docs")

PATTERNS = [
    re.compile(r"(\[[^\]]+\]\()\/(guides|frameworks|release-notes)\/([a-z0-9-]+)\/(#[^)\s]+)?\)"),
    re.compile(r"(href=\")\/(guides|frameworks|release-notes)\/([a-z0-9-]+)\/(#[^\"]+)?\""),
]


def relativize(src_file: Path, section: str, page: str) -> str:
    src_dir = src_file.parent.resolve()
    target = (ROOT / section / f"{page}.md").resolve()
    rel = os.path.relpath(target, start=src_dir).replace("\\", "/")
    if not rel.startswith((".", "/")):
        rel = f"./{rel}"
    return rel


def process_file(path: Path) -> tuple[bool, int]:
    text = path.read_text(encoding="utf-8")
    original = text
    n = [0]

    def md_repl(m: re.Match) -> str:
        prefix, section, page, fragment = m.group(1), m.group(2), m.group(3), m.group(4) or ""
        rel = relativize(path, section, page)
        n[0] += 1
        return f"{prefix}{rel}{fragment})"

    def attr_repl(m: re.Match) -> str:
        prefix, section, page, fragment = m.group(1), m.group(2), m.group(3), m.group(4) or ""
        rel = relativize(path, section, page)
        n[0] += 1
        return f'{prefix}{rel}{fragment}"'

    text = PATTERNS[0].sub(md_repl, text)
    text = PATTERNS[1].sub(attr_repl, text)

    if text != original:
        path.write_text(text, encoding="utf-8")
        return True, n[0]
    return False, 0


def main() -> None:
    total_files = 0
    total = 0
    for ext in ("*.md", "*.mdx"):
        for f in ROOT.rglob(ext):
            changed, n = process_file(f)
            if changed:
                total_files += 1
                total += n
                print(f"  {f.relative_to(ROOT)}: {n} link(s) rewritten")
    print(f"\nTotal: {total} replacement(s) across {total_files} file(s).")


if __name__ == "__main__":
    main()
