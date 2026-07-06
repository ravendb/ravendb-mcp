# RQL cheat-sheet

Dense syntax reference. Method surface: `rql://functions`. Concepts: `rql://reference`. Version-correct docs: `rql://docs/{version}` (linked at bottom).

## Clause order
```
declare <fn>
from <source> [as alias]
[ group by <fields> ]
[ where <predicate> ]
[ order by <field> [as <type>] [asc|desc] ]
[ load <ref> as x ]
[ filter <predicate> [ filter_limit <n> ] ]
[ select <projection> ]
[ include <related> ]
[ limit <skip>, <take> | limit <take> offset <skip> ]
```
Comments: `// line`, `/* block */`. Strings: `'…'` or `"…"`. Literals: `true false null`, numbers. Params: `$name`. Quote names with special chars: `from "Order Lines"`.

## from
```
from Users                     # collection
from "Order Lines"             # quoted name
from @all_docs                 # every document
from index 'Orders/Totals'     # named static index
from Users as u                # alias (needed for load / JS projections)
```

## where
```
where Age > 30 and IsActive = true          # = == , != <>, < <= > >=
where Address.City = 'Albuquerque'          # nested path via dot
where Age between 18 and 65
where Country in ('US','UK')                 # any listed value
where Lines[].Sku all in ('A','B')           # array field: all listed present
where startsWith(Name,'Jo')  where endsWith(Email,'@acme.com')
where exists(MiddleName)                      # anchor optional fields
where regex(Sku,'^A-\d+$')
where search(Bio,'raven database')            # full-text; see rql://fulltext
where id() = 'users/1-A'
where boost(Name = 'Bob', 10)
```
`not` cannot START a where clause — follow an expression: `exists(X) and not X in (…)`. Full method list: `rql://functions`.

## order by
```
order by Age                     # default asc
order by Amount as double desc   # types: string | long | double | alphaNumeric
order by UnitsInStock as long desc, score(), Name   # chained (Corax: max 16)
order by score()  |  random()  |  random(1234)       # relevance / random / seeded
order by spatial.distance(...)                        # see rql://spatial
order by custom(Field,'MySorter')                     # Lucene engine only
```

## select (projection) — applied last, results not session-tracked
```
select Name, Address.City as City         # fields + aliases
select distinct Country
select { Full: x.FirstName + ' ' + x.LastName,
         Total: x.Lines.map(l => l.Price*l.Qty).reduce((a,b)=>a+b,0) }   # JS object
select { Meta: getMetadata(x) }            # metadata access
```

## group by / aggregation
```
from Orders
group by ShipTo.City
select ShipTo.City as City, count() as N, sum(Amount) as Total
```
Dynamic `group by` supports **only `count()` and `sum()`** (avg/min/max error — use a facet, map-reduce index, or JS projection). `group by array(Tags)` keys on whole array; `key()` returns the group key. Sort aggregates: `order by count() as long`. Aggregation auto-creates an `Auto/…` index.

## include / load (avoid N+1)
```
load o.CustomerId as c select o.Id, c.Name
include CustomerId, Lines[].ProductId       # related docs, one round-trip
include counters(o,'Views')
include timeseries('HeartRate', $from, $to)
include revisions('2026-02-23T07:40:54Z')
```

## filter (post-index scan)
`filter <predicate>` runs AFTER the index query, scanning retrieved results server-side — for exact checks the index can't answer. `where` is the primary filter; cap the scan with `filter_limit <n>`.

## paging
```
limit 25            # take 25
limit 50, 25        # skip 50, take 25
limit 25 offset 50  # take 25, skip 50
```
MCP `run_query` caps `pageSize` at 128; page with `start`.

## gotchas (these cause errors, not just bad results)
- **Clause order is fixed:** `from → group by → where → order by → load → filter → select → include → limit`. `order by` **before** `load` and `select`, never after.
- **`facet(...)` and `morelikethis(...)` need a static index** — `from index 'Name' select facet(...)`, never `from Collection`. `suggest()` and `highlight()` also work on dynamic queries (highlighting not with a dynamic `group by`).
- Dynamic `group by` aggregates: **only `count()`/`sum()`** — avg/min/max need a facet, map-reduce index, or JS projection.
- Numeric/date `order by` needs a cast (`as double`/`as long`), else lexical order.
- `not` cannot start a `where`; anchor with `exists(field)` or a positive term first.

## read-only note
`update { … }` / patch queries are rejected by this server.

## More detail — local first, official docs last
Read these condensed local resources before going to the internet:
- Concepts (dynamic vs index, staleness, gotchas): `rql://reference`
- Every method/operator with signatures: `rql://functions`
- Full-text: `rql://fulltext` · Spatial: `rql://spatial` · Time series: `rql://timeseries` · Facets: `rql://facets` · Suggestions/MLT/highlighting/vector: `rql://advanced`

Last resort — official version-correct docs (this resource holds the actual URLs): `rql://docs/{6.2|7.1|7.2}`.
