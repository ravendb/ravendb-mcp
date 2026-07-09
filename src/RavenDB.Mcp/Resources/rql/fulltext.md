# RQL — full-text search

Tokenized text search over analyzed fields. Case-insensitive with the default analyzer. Dynamic queries auto-create a full-text auto-index; for heavy or custom-analyzer use, define a static index and query it with `from index '…'`. Version-correct details: `rql://docs` → "Full-Text Search".

## search(field, terms [, and|or])
```
from Companies where search(Notes, 'University Sales Japanese')      # terms default to OR
from Companies where search(Notes, 'College German', and)           # require ALL terms
```
- Third arg sets the operator **between the terms** in this one call (`or` default, or `and`).
- The operand is an analyzed field; `search` tokenizes both the field value and the query string. This is different from `=`/`startsWith`, which match the raw (non-analyzed) value.

### Combining multiple search() calls
Join separate `search()` calls with an explicit `or`/`and` (raw RQL requires the operator; only the client API defaults to OR):
```
from Companies where search(Address.Country,'France') or search(Name,'Markets')
from Employees where search(Notes,'French') and (exists(Title) and not search(Title,'Manager'))
```

### Wildcards (inside the term string)
```
where search(Notes,'art*')     # prefix match
where search(Notes,'*logy')    # postfix match — triggers a FULL INDEX SCAN (avoid on hot paths)
where search(Notes,'*mark*')   # contains — also scan-heavy
```

## Relevance scoring
```
order by score()                              # sort best matches first (order by only — score() is NOT valid in select)
```
The score value itself lives in each result's `@metadata` under `@index-score` (run_query returns it with `metadata:true`, or project `select { Meta: getMetadata(x) }`).

### boost(predicate, factor)
Multiply the relevance contribution of a clause; results auto-order by score.
```
from Companies
where boost(startsWith(Name,'O'), 10)
   or boost(startsWith(Name,'P'), 50)
   or boost(endsWith(Name,'OP'), 90)
```

## Approximate matching
```
where fuzzy(Name = 'Bob', 0.7)                      # edit-distance match, factor 0.0..1.0 (closer to 1 = stricter)
where proximity(search(Bio,'raven database'), 3)    # the searched terms within 3 words of each other
```

## exact(predicate) — bypass the analyzer
Force case-sensitive, non-analyzed matching:
```
from Employees where exact(FirstName == 'Robert')
from Orders    where exact(Lines.ProductName == 'Teatime Chocolate Biscuits')
```

## lucene(field, 'query') — raw Lucene (Lucene engine only)
```
where lucene(Name, 'Jo* AND -Johnson')
```

## Notes & gotchas
- **Engine matters.** Corax (default in recent versions) vs Lucene differ on `lucene()` support and some analyzer/wildcard behavior — confirm via `rql://docs`.
- Postfix/contains wildcards force a full scan; prefer prefix (`term*`) or a purpose-built static index.
- `search` ≠ `startsWith`/`=`: use `search` for analyzed text relevance, the others for exact/prefix on raw values.
- For "did you mean" and similar-document retrieval see `rql://advanced` (suggestions, more-like-this).
