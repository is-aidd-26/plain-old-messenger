#!/usr/bin/env python3
"""Рендерит mermaid-диаграммы из docs/architecture/ в PNG рядом с md-файлами."""

import subprocess
import sys
import tempfile
from pathlib import Path

ARCH_DIR = Path(__file__).resolve().parent.parent / "docs" / "architecture"
PUPPETEER_CONFIG = '{"args": ["--no-sandbox", "--disable-setuid-sandbox"]}'
MMDC = ["npx", "-y", "-p", "@mermaid-js/mermaid-cli", "mmdc"]


def render(md: Path, config: str) -> bool:
    png = md.with_suffix(".png")
    suffixed = md.with_name(f"{md.stem}-1.png")
    png.unlink(missing_ok=True)
    suffixed.unlink(missing_ok=True)
    try:
        subprocess.run(
            [*MMDC, "-p", config, "-b", "white", "-s", "2", "-i", md, "-o", png],
            check=True,
            capture_output=True,
            text=True,
        )
    except subprocess.CalledProcessError as error:
        print(f"FAIL {md.name}: {error.stderr}", file=sys.stderr)
        return False
    if suffixed.exists():
        suffixed.replace(png)
    print(f"OK   {png.name}")
    return png.exists()


def main() -> int:
    files = sorted(ARCH_DIR.glob("*.md"))
    diagrams = [md for md in files if "```mermaid" in md.read_text(encoding="utf-8")]
    if not diagrams:
        print(f"Нет mermaid-диаграмм в {ARCH_DIR}", file=sys.stderr)
        return 1
    with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False) as config:
        config.write(PUPPETEER_CONFIG)
    try:
        results = [render(md, config.name) for md in diagrams]
    finally:
        Path(config.name).unlink(missing_ok=True)
    return 0 if all(results) else 1


if __name__ == "__main__":
    sys.exit(main())
