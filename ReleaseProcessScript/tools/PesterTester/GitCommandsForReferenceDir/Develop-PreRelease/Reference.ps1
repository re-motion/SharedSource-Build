git checkout develop
git checkout -b "prerelease/v1.2.0-alpha.1"
git tag -a "v1.2.0-alpha.1" -m "v1.2.0-alpha.1"
git checkout develop
git merge "prerelease/v1.2.0-alpha.1" --no-ff