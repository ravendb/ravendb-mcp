# RQL — time series

Query time series attached to documents, inline or via a named declaration. Version-correct details: `rql://docs` → "Querying Time Series".

## Two entry forms
Inline projection:
```
from Employees as e
where Birthday < '1994-01-01'
select timeseries( from HeartRates )
```
Named declaration (reusable, with range/aggregation):
```
declare timeseries ts(jogger) {
  from jogger.HeartRates
  between '2020-05-27T00:00:00Z' and '2020-06-23T00:00:00Z'
  group by '1 hour'
  select min(), max(), avg(), first(), last()
}
from Users as jogger
where Age > 30
select ts(jogger)
```

## Inner time-series query language
| Part | Purpose |
|---|---|
| `from <SeriesName>` | the series source (`from doc.Series` inside a declared block) |
| `between <ts> and <ts>` | ISO-8601 range; or params `$from` / `$to` |
| `first <n> <unit>` / `last <n> <unit>` | earliest / most-recent window (e.g. `last 30 minutes`); mutually exclusive with `between` |
| `where Values[n] <op> x` | filter by the n-th value in each entry |
| `where Tag == '…'` | filter by entry tag |
| `group by '<n> <unit>'` | time buckets; secondary group by tag: `group by '1 hour', tag` |
| `select <aggregations>` | per-bucket aggregation |
| `scale <double>` | multiply every value |
| `offset <timespan>` | shift timestamps to a timezone (client-exposed on some versions) |

Bucket units: `ms`/`milliseconds` · `second` · `minute` · `hour` · `day` · `month` · `quarter` · `year` (e.g. `'7 days'`, `'1 hour'`).

## Aggregations
`min() max() sum() avg() first() last() count() percentile(<n>) stddev() slope()`

## Worked examples
Filter by a value column, weekly max/min:
```
from Companies as c
select timeseries(
  from StockPrices
  where Values[4] > 500000
  group by '7 days'
  select max(), min()
)
```
Most-recent window with the `last` clause (a range clause right after `from` — NOT a function, NOT in `where`):
```
declare timeseries recent(u) {
  from u.HeartRates
  last 30 minutes                   # or: first 7 days
  select avg(), max()
}
from Users as u select recent(u)
```

## Fetch one document's raw series
When you want a single document's series (not a query projection), prefer `get_document_data` with `include=TimeSeries` + `timeSeriesName` (and optional `from`/`to`).
