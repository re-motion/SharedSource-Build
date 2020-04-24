git checkout -b "prerelease/v1.2.0-rc.1" *>$NULL
git checkout "release/v1.2.0" *>$NULL
git merge "prerelease/v1.2.0-rc.1" *>$NULL
git tag -a "v1.2.0-rc.1" -m "v1.2.0-rc.1" *>$NULL