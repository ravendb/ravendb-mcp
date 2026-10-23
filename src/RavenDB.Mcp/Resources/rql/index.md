# RQL guide: start here (this cluster: v{VERSION})

Read-only RQL over this server. Progressive path: **this index → `skill://ravendb/references/rql-cheatsheet.md` → the ONE feature reference you need → official docs (`rql://docs`) only as a last resort.**

**The keywords below tell you what EXISTS, not the syntax. For anything you use, open its reference and follow the exact syntax there. Do not write RQL from memory; assume your recall is stale. Features above the 6.2 baseline are marked `(7.0+)` or `(7.1+)` where they are documented; this cluster is v{VERSION}.**

Don't know the document shape yet? Run `from <Collection> limit 1` through `run_query` first to see the real field names before you filter on them.

## Index: find your keyword, then read that reference
| You want… (keywords) | Read |
|---|---|
| Clause order, operators, paging; `from` `where` `filter` `in` `all-in` `between` `not` | `skill://ravendb/references/rql-cheatsheet.md` |
| Sorting: `order by` `asc` `desc` `cast` `score` `random` `custom` | `skill://ravendb/references/rql-cheatsheet.md` |
| Projections: `select` `distinct` `as` `projection` `js` `declare` `load` | `skill://ravendb/references/rql-cheatsheet.md` |
| Group-by aggregation: `count` `sum` `key` `array` | `skill://ravendb/references/rql-cheatsheet.md` |
| How querying works: dynamic vs index, auto-indexes, staleness, gotchas | `skill://ravendb/references/rql-reference.md` |
| Any method + exact signature; `id` `exists` `regex` `startsWith` `endsWith` `intersect` `cmpxchg` `lucene` | `skill://ravendb/references/rql-functions.md` |
| Full-text: `search` `boost` `fuzzy` `proximity` `exact` wildcards | `skill://ravendb/references/rql-fulltext.md` |
| Spatial: `spatial.within` `circle` `wkt` `distance` | `skill://ravendb/references/rql-spatial.md` |
| Time series: `select/declare timeseries`, buckets, aggregations | `skill://ravendb/references/rql-timeseries.md` |
| Facets, and `avg`/`min`/`max` over a collection: `facet(...)` | `skill://ravendb/references/rql-facets.md` |
| `suggest` `morelikethis` `highlight` `vector.search` (7.0+) | `skill://ravendb/references/rql-advanced.md` |
| Includes: `include` `counters` `timeseries` `revisions` `highlight` | `skill://ravendb/references/rql-cheatsheet.md` (+ `rql-advanced.md` for highlight) |
| Official version-correct docs, last resort | `rql://docs` |

Don't see your keyword? It may be version-specific: check `skill://ravendb/references/rql-functions.md`, then `rql://docs`.

Beyond queries (client code, indexes, modelling, diagnostics), start at `skill://ravendb/SKILL.md`.
