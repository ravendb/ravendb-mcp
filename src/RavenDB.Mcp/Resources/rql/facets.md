# RQL — faceted (aggregated) search

Bucketed counts/aggregations over indexed fields — the efficient way to summarize/navigate a large result set, and how to get `avg`/`min`/`max` over a collection (which dynamic `group by` cannot). Requires a **static index** on the faceted fields. Version-correct details: `rql://docs` → "Perform a Faceted (Aggregated) Search".

## Value facet
```
from index 'Cameras/ByFeatures'
select facet(Brand) as 'Camera Brand'
```
Returns each distinct value of `Brand` with a document count.

## Range facet
```
from index 'Cameras/ByFeatures'
select facet(
  Price < 200.0,
  Price >= 200.0 and Price < 400.0,
  Price >= 400.0 and Price < 600.0,
  Price >= 600.0)
```

## Aggregations inside a facet
`sum avg min max count` per bucket:
```
from index 'Cameras/ByFeatures'
select facet(Brand,
             sum(UnitsInStock),
             avg(Price),
             min(Price),
             max(MegaPixels),
             max(MaxFocalLength))
```
Range facet + aggregations, parameterized:
```
from index 'Cameras/ByFeatures'
select facet(Price < $p0,
             Price >= $p1 and Price < $p2,
             sum(UnitsInStock), avg(Price), min(Price), max(MegaPixels))
{ "p0": 200.0, "p1": 200.0, "p2": 400.0 }
```

## Options
Pass a JSON parameter to a value facet:
```
from index 'Cameras/ByFeatures'
select facet(Brand, $options)
{ "options": { "PageSize": 3, "TermSortMode": "CountDesc" } }
```
| Option | Values |
|---|---|
| `PageSize` | int — cap the number of buckets returned |
| `TermSortMode` | `ValueAsc` · `ValueDesc` · `CountAsc` · `CountDesc` |

## From a stored FacetSetup document
Reuse a `FacetSetup` document instead of inlining facets:
```
from index 'Cameras/ByFeatures'
select facet(id('customFacetSetupId'))
```

## Notes & gotchas
- Facets query a **static index**, not a collection — create/verify it via `get_index` first.
- Use facets for navigation/summary UIs and for avg/min/max over a collection; use dynamic `group by` (`rql://cheatsheet`) only for ad-hoc `count`/`sum`.
