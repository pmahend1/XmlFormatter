#!/usr/bin/env python3
"""Build the coverage comment posted to a pull request.

Reads the Cobertura report produced for the PR head and, when it is available, the one
produced for the PR base, and writes coverage-comment.md.

The base report is best-effort by design: the workflow step that produces it is allowed to
fail, because a base commit that no longer builds should cost the PR its delta, not its
whole coverage report. With no base to compare against, the comment reports head coverage
alone rather than reporting nothing.
"""

from __future__ import annotations

import glob
import os
import xml.etree.ElementTree as ET

MARKER = "<!-- coverage-report -->"

HEAD_REPORTS = "head-coverage/**/coverage.cobertura.xml"
BASE_REPORTS = "base/XmlFormatter.Tests/TestResults/**/coverage.cobertura.xml"


def read_rates(pattern: str) -> tuple[float, float] | None:
    """Return (line_rate, branch_rate) as percentages, or None if no report matched.

    Sums the counts across every matching report rather than reading one file's rate. A run
    can leave more than one report behind - a rerun, or a second test project later - and
    picking one of them silently reports a number for part of the suite.
    """
    matches = sorted(glob.glob(pattern, recursive=True))
    if not matches:
        return None

    lines_covered = lines_valid = branches_covered = branches_valid = 0
    for path in matches:
        try:
            root = ET.parse(path).getroot()
        except (ET.ParseError, OSError):
            continue
        lines_covered += int(root.get("lines-covered", 0))
        lines_valid += int(root.get("lines-valid", 0))
        branches_covered += int(root.get("branches-covered", 0))
        branches_valid += int(root.get("branches-valid", 0))

    if lines_valid == 0:
        return None

    line = lines_covered / lines_valid * 100
    # A project with no branches at all is 100% branch-covered, not 0%.
    branch = branches_covered / branches_valid * 100 if branches_valid > 0 else 100.0
    return line, branch


def format_delta(head: float, base: float) -> str:
    delta = head - base
    # Coverage percentages are noisy in the last decimal; below that a "change" is rounding.
    if abs(delta) < 0.05:
        return "no change"
    return f"{delta:+.1f} pp"


def build_row(label: str, head: float, base: float | None) -> str:
    if base is None:
        return f"| {label} | {head:.1f}% | - | - |"
    return f"| {label} | {head:.1f}% | {base:.1f}% | {format_delta(head, base)} |"


def main() -> int:
    head = read_rates(HEAD_REPORTS)
    if head is None:
        print("No head coverage report found; not writing a comment.")
        return 0

    base = read_rates(BASE_REPORTS)
    head_line, head_branch = head
    base_line, base_branch = base if base else (None, None)

    lines = [
        MARKER,
        "### Coverage",
        "",
        "| | This PR | Base | Change |",
        "|:---|---:|---:|---:|",
        build_row("Line", head_line, base_line),
        build_row("Branch", head_branch, base_branch),
        "",
    ]

    if base is None:
        lines.append(
            "_No base measurement available, so no comparison - this is the PR's own coverage._"
        )
        lines.append("")

    run_url = (
        f"{os.environ.get('GITHUB_SERVER_URL', 'https://github.com')}/"
        f"{os.environ.get('GITHUB_REPOSITORY', '')}/actions/runs/"
        f"{os.environ.get('GITHUB_RUN_ID', '')}"
    )
    lines.append(f"[Full HTML report]({run_url}) is attached to the run as `coverage-report`.")

    with open("coverage-comment.md", "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines) + "\n")

    print(f"line {head_line:.1f}%, branch {head_branch:.1f}%")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
