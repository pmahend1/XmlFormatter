# XmlFormatter.Benchmarks

Perf harness for the formatter. Three verbs: `generate`, `bench`, `guard`.

```bash
dotnet build -c Release
dotnet run -c Release --project XmlFormatter.Benchmarks -- generate
dotnet run -c Release --project XmlFormatter.Benchmarks -- bench --save perf/baseline.json
# ...change the formatter, rebuild...
dotnet run -c Release --project XmlFormatter.Benchmarks -- bench --compare perf/baseline.json
dotnet run -c Release --project XmlFormatter.Benchmarks -- guard
```

Always `-c Release`. A Debug build measures the wrong thing; `bench` warns if that is all
it can find.

## Why this exists

The baseline suite in `XmlFormatter.Tests` compares formatter output byte for byte. That
catches behaviour changes and nothing else. The `O(n²)` sibling traversal fixed in #37 made
a 9.6 MB file take 16 seconds instead of 1.2, and produced **byte-identical output** the
whole time - no output comparison could ever have flagged it. Cost has to be measured
separately, which is what this project is for.

## generate

Writes the sample corpus under `perf/`, all of it gitignored:

| Path                           | What                                                                  |
|--------------------------------|-----------------------------------------------------------------------|
| `perf/samples/*.xml`           | the size ladder: 0.8 KB → 9.6 MB, minified                            |
| `perf/samples/formatted/*.xml` | the same ladder after one formatting pass                             |
| `perf/samples/shapes/*.xml`    | one document per code path: deep, attributes, comments, unicode, text |

Everything is derived from its index - no RNG - so two machines produce byte-identical
files and timings stay comparable. That is also why none of it is committed: ~40 MB of
output reproducible from a generator already in the repo is pure duplication.

**The formatted ladder is not redundant.** Under default options whitespace nodes are
discarded at load, so a minified and an indented copy of a document produce the same DOM
and the same timing. Under `PreserveNewLines` they do not - the indented copy keeps its
whitespace nodes, roughly doubling the sibling count. A minified-only corpus silently
understates the case the editor actually hits, which is re-formatting an already formatted
file.

## bench

Times the formatter end to end by shelling out to `XmlFormatter.CommandLine`, once per run.

A subprocess per run is deliberate - it is what the editor extension does, and it gives
every measurement a cold JIT and an empty heap rather than letting an earlier sample's
warm-up flatter a later one. The cost is a fixed .NET host startup on every number, so
startup is re-measured with a 1 KB document immediately before each sample and subtracted.
A busy machine then skews both halves together instead of inflating the reported work.

```shell
bench [--samples DIR] [--options NAME] [--save FILE] [--compare FILE]
```

`--options` is `default`, `preserve-newlines` or `blank-lines`; the names match
`XmlFormatter.Tests.OptionSets` where they overlap, so a number here and a baseline there
refer to the same settings.

The scaling summary only prints for an ascending size ladder. The shape corpus is five
unrelated documents in alphabetical order - comparing neighbours there produces ratios that
look alarming and mean nothing.

## guard

The check that can actually fail a build on a performance regression.

It formats the same document at two sizes and asserts that work grows no faster than input,
within a tolerance (default 1.5x linear). Ratios, not absolute times: a slow CI runner moves
both measurements together and the ratio survives. It runs in-process rather than through
the CLI so both halves share one JIT and one heap, and takes the fastest of five runs -
noise only ever adds time, so the quickest run is closest to the real work.

Verified against the bug it exists to catch: at the guard's two sizes the pre-#37 formatter
measures **3.43x linear** on the default path. The current one measures 0.73x.

### Known-failing cases

A case can be marked `KnownFailing`. It is still measured and still printed, but it does not
fail the run - a guard that is red from the day it lands stops meaning anything. If such a
case starts passing, the run says `FIXED` and asks for the flag to be removed, so the
exemption cannot quietly outlive the bug.

One case is flagged today: `orders/preserve-newlines` measures ~3.5x linear. The `ChildNodes`
fix in #37 did not reach that path. See `AGENTS.md`.
