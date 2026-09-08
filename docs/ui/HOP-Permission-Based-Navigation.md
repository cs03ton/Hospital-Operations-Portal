# HOP Permission-Based Navigation

Navigation items declare permission codes in the registry. Visibility is an ordered union of direct and effective delegated permissions, deduplicated by route. Role-name checks are not used for Fleet authorization.

Task badges reuse the module dashboard query, hide zero and cap visual output at `99+`. A badge is advisory; opening the route always executes the route guard and backend authorization. Delegated navigation refreshes periodically and on window focus so expiry does not require signing in again.

This Fleet integration is additive. Leave and other module navigation continue using their existing permission context and behavior.
