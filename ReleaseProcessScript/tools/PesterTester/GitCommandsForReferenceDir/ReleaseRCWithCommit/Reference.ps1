git checkout -b "prerelease/v1.2.0-rc.1"
git commit -m "Commit on prerelease branch" --allow-empty
git tag -a "v1.2.0-rc.1" -m "v1.2.0-rc.1"
git checkout "release/v1.2.0"
git merge "prerelease/v1.2.0-rc.1" --no-ff