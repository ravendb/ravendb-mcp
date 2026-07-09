# RQL — advanced (suggestions, more-like-this, highlighting, vector search)

Version-correct details: `rql://docs`.

## Suggestions — `suggest(field, term [, optionsJson]) [as alias]`
"Did you mean" over an indexed field.
```
from Products
select suggest(Name, 'chaig',
  '{ "Accuracy": 0.4, "PageSize": 5, "Distance": "JaroWinkler", "SortMode": "Popularity" }') as 'Names'

from Companies
select suggest(Name, 'chop-soy china'), suggest(Contact.Name, 'maria larson')   # multiple fields

from Products select suggest(Name, $terms) { "terms": ["chaig","tof"] }          # multiple terms via param
```
| Option | Values / default |
|---|---|
| `Accuracy` | float, default `0.5` |
| `PageSize` | int, default `15` |
| `Distance` | `Levenshtein` (default) · `JaroWinkler` · `NGram` · `None` |
| `SortMode` | `Popularity` · `None` |

## More-like-this — `morelikethis(seed, optionsJson)`
Find documents similar to a seed doc; used in `where`, over an index.
```
from index 'Articles/MoreLikeThis'
where morelikethis(id() = 'articles/1', '{ "Fields": ["Body"] }')

from index 'Articles/MoreLikeThis'
where morelikethis(id() = 'articles/1', '{ "Fields": ["Body"] }') and Category == 'IT'   # extra filter
```
| Option | Meaning |
|---|---|
| `Fields` | string[] — fields to compare |
| `MinimumTermFrequency` | ignore terms rarer than this in the seed |
| `MinimumDocumentFrequency` | ignore terms rarer than this across the corpus |
| `MaximumQueryTerms` | cap generated query terms |
| `Boost` / `BoostFactor` | weight terms by relevance |

## Highlighting — `highlight(field, fragmentLength, fragmentCount [, optionsJson])`
Return matched fragments; placed in an `include`, pairs with `search()`.
```
from index 'Employees/ByNotes'
where search(EmployeeNotes, 'manager')
include highlight(EmployeeNotes, 35, 2)                       # field, fragmentLength (chars), fragmentCount

from index 'ContactDetailsPerCountry'
where search(ContactDetails, 'agent')
include highlight(ContactDetails, 35, 2, $opt) { "opt": { "GroupKey": "Country" } }   # map-reduce index
```

## Vector / AI search — `vector.search(...)`  (7.x+, NOT 6.2)
```
vector.search(<field | embedding-fn>, <queryValue> [, minimumSimilarity] [, numberOfCandidates])
```
- `minimumSimilarity`: float 0.0–1.0 (optional; server default).
- `numberOfCandidates`: int, max vectors examined in graph search (optional; server default).
```
from Products where vector.search(embedding.text(Name), 'italian food', 0.82, 20)
from Products where vector.search(embedding.text_i8(Name), $searchTerm)
from Movies   where vector.search(TagsEmbeddedAsSingle, $queryVector, 0.85, 10)
from Products where vector.search(embedding.text(Name), embedding.forDoc($docId), 0.82)
from Products where (PricePerUnit > $min) and vector.search(embedding.text(Name), $term, 0.75, 16)
from index MyIndex where vector.search(FieldName, $query, 0.8, 16)
```
Embedding wrappers (declare source type / quantization):
| Wrapper | Source → storage |
|---|---|
| `embedding.text(f)` | text → float32 |
| `embedding.text_i8(f)` | text → int8 |
| `embedding.text_i1(f)` | text → binary |
| `embedding.f32_i8(f)` / `embedding.f32_i1(f)` | float32 → int8 / binary |
| `embedding.i8(f)` / `embedding.i1(f)` | pre-quantized (no re-quantization) |
| bare `field` | raw float32 vector |

Query-value forms: text literal / `$param` · raw vector `$queryVector` (JSON `@vector`) · base64 string · `embedding.forDoc($id)` · `embedding.text(Field, ai.task('task-id'))`. This is a 7.x feature — confirm availability with `rql://docs`.
