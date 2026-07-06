# RQL guide — start here (this cluster: v{VERSION})

Read-only RQL over this server. Progressive path: **this index → the ONE feature resource you need → official docs (`rql://docs`) only as a last resort.**

**The keywords below tell you what EXISTS, not the syntax. For anything you use, open its resource and follow the exact syntax there — do not write RQL from memory; assume your recall is stale. The resources are the source of truth for v{VERSION}.**

## Index — find your keyword, then read that resource
| You want… (keywords) | Read |
|---|---|
| Clause order, operators, paging; `from` `where` `filter` `in` `all-in` `between` `not` | `rql://cheatsheet` |
| Sorting: `order by` `asc` `desc` `cast` `score` `random` `custom` | `rql://cheatsheet` |
| Projections: `select` `distinct` `as` `projection` `js` `declare` `load` | `rql://cheatsheet` |
| Group-by aggregation: `count` `sum` `key` `array` | `rql://cheatsheet` |
| How querying works: dynamic vs index, auto-indexes, staleness, gotchas | `rql://reference` |
| Any method + exact signature; `id` `exists` `regex` `startsWith` `endsWith` `intersect` `cmpxchg` `lucene` | `rql://functions` |
| Full-text: `search` `boost` `fuzzy` `proximity` `exact` wildcards | `rql://fulltext` |
| Spatial: `spatial.within` `circle` `wkt` `distance` | `rql://spatial` |
| Time series: `select/declare timeseries`, buckets, aggregations | `rql://timeseries` |
| Facets, and `avg`/`min`/`max` over a collection: `facet(...)` | `rql://facets` |
| `suggest` `morelikethis` `highlight` `vector.search` (7.x+) | `rql://advanced` |
| Includes: `include` `counters` `timeseries` `revisions` `highlight` | `rql://cheatsheet` (+ `rql://advanced` for highlight) |
| Official version-correct docs — last resort | `rql://docs` |

Don't see your keyword? It may be version-specific — check `rql://functions`, then `rql://docs`.
