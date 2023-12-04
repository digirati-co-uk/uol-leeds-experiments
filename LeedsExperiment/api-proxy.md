# API Proxy

The *Preservation* application contains a route that proxies Fedora'a API, but _without the content negotiation_.

The purpose of this is purely for learning and exploring, not for production use. It's difficult to see content-negotiated responses.

NB This will not be part of a production Fedora API wrapper! At least, not without an additional layer of OAuth2 protection.

Examples:

Given this Fedora resource (username and password upplied separately):

https://uol.digirati.io/fcrepo/rest/tom_test_basic_container

...we can view different representations of it:

- https://uol.digirati.io/api/fedora/application/ld+json/tom_test_basic_container
- https://uol.digirati.io/api/fedora/text/plain/tom_test_basic_container
- https://uol.digirati.io/api/fedora/text/turtle/tom_test_basic_container
- https://uol.digirati.io/api/fedora/text/n3/tom_test_basic_container
- https://uol.digirati.io/api/fedora/application/rdf+xml/tom_test_basic_container
- https://uol.digirati.io/api/fedora/application/n-triples/tom_test_basic_container

Fedora defaults to an **expanded** json-ld representation, but here I am overriding that and showing the **compacted** representation. But you can see others, like this:

- https://uol.digirati.io/api/fedora/application/ld+json/tom_test_basic_container?jsonld=flattened
- https://uol.digirati.io/api/fedora/application/ld+json/tom_test_basic_container?jsonld=expanded

The compacted representation, https://uol.digirati.io/api/fedora/application/ld+json/tom_test_basic_container?jsonld=compacted (or just leave off the query string parameter), is the representation we would map to types in our API wrapper.

Shown here with `@context` collapsed:

<img width="566" alt="image" src="https://github.com/digirati-co-uk/uol-leeds-experiments/assets/1443575/db72689a-d200-4359-946f-ac373ac6bac2">


