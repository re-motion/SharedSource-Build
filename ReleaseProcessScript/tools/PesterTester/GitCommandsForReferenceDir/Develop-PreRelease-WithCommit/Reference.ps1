git checkout develop *>$NULL
git checkout -b "prerelease/v1.2.0-alpha.1" *>$NULL
git commit -m "Commit on prerelease branch" --allow-empty *>$NULL
git tag -a "v1.2.0-alpha.1" -m "v1.2.0-alpha.1" *>$NULL
git checkout develop *>$NULL
git merge "prerelease/v1.2.0-alpha.1" --no-ff *>$NULL