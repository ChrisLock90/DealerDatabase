Dealer Database – Decisions

Matching: Records are matched using a combination of exact identifiers and corroborating fields. Company/VAT identifiers are strongest; otherwise names, postcodes, addresses and contact details are compared using normalisation and fuzzy matching.

Same dealer: Two records are treated as the same dealer only when there is strong evidence of identity, such as a shared authoritative identifier or multiple independent matching signals. Fuzzy matching alone is only accepted where there is sufficient supporting evidence to reduce false positives.

Normalisation: Names, phone numbers, emails, domains, postcodes and identifiers are normalised before comparison so that differences in formatting do not prevent genuine matches.

Conflict resolution: Source priority is used when sources disagree. Companies House is preferred for legal/company information, the relevant regulator is preferred for regulatory information, and commercial/crawled sources are preferred where they provide more appropriate current trading or marketplace information.

Data completeness: Where the preferred source has no value, a lower-priority source may be used rather than leaving the field empty. Source values are retained so that the result remains traceable.

Directors: Directors from the same dealer are merged using normalised name and role information. Non-empty values are retained where one source contains information missing from another.

Assumptions: Company and regulatory identifiers are generally reliable. Crawled web data is treated as the least reliable source because it may be incomplete, duplicated or out of date. A dealership is treated as a single dealer entity unless there is evidence that records represent different businesses.

Scope: The import produces a current consolidated view rather than maintaining a full history of every previous value or business relationship.

Not implemented: Automatic franchise/group hierarchy detection, machine-learning-based matching and a manual match-review workflow were left out to keep the solution focused and explainable.

Next steps: With more time, I would add a review queue for uncertain matches, historical change tracking, stronger address/business-group matching and additional automated validation of source data.