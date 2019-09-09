git checkout -b "release/v1.1.1"
git checkout "support/v1.1"
git merge "release/v1.1.1" --no-ff
git tag -a "v1.1.1" -m "v1.1.1"
git branch "hotfix/v1.1.2"