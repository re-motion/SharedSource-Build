git checkout -b "prerelease/v1.2.0-rc.1"
git checkout "release/v1.2.0"
git merge "prerelease/v1.2.0-rc.1"
git tag -a "v1.2.0-rc.1" -m "v1.2.0-rc.1"