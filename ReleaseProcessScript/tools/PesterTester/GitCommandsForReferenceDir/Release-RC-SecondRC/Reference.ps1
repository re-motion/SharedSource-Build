git checkout -b "prerelease/v1.2.0-rc.2" *>$NULL
git commit -m "Another commit on prerelease" --allow-empty *>$NULL
git tag -a "v1.2.0-rc.2" -m "v1.2.0-rc.2" *>$NULL
git checkout "release/v1.2.0" *>$NULL
git merge "prerelease/v1.2.0-rc.2" --no-ff *>$NULL