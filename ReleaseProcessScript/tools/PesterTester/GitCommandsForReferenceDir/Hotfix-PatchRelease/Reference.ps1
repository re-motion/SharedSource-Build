git checkout -b "release/v1.1.1" *>$NULL
git checkout "support/v1.1" *>$NULL
git merge "release/v1.1.1" --no-ff *>$NULL
git tag -a "v1.1.1" -m "v1.1.1" *>$NULL
git branch "hotfix/v1.1.2" *>$NULL