# Contributing

So you want to contribute to R2API? Great! We invite all to contribute to R2API to make modding
easier for mod developers. Here are a few guidelines in order to make your contribution as easy as possible!

## Guidelines
To be able to maintain the project without breaking too much of a sweat, we try to stick to the following rules:

- No implementation of any in-game functionality - even if it's for easier testing. Release it as a separate plugin.
- The MonoMod layer may only be used to allow hooking methods that may otherwise be inlined. Under no circumstances
should the monomod layer ever rely on the actual plugin being loaded.
- Duplicating code from Assembly-CSharp should be avoided whenever possible.
- Non exposed code should be cleaned up each commit. There's no point in keeping it around as comments - that's what
VCS is for.
- Since removing released exposed code is only possible in a major release, candidates should be marked as obsolete but
not removed.

## Pull Requests
Pull requests are welcomed but may be rejected for the reasons mentioned above. Please try to maintain high code quality
and use the .editorconfig that is included in the repo. PRs with poor code quality may be denied.
