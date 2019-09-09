git checkout -b release/v1.2.0
git commit -m "Commit on release branch" --allow-empty
git checkout master --quiet
git merge release/v1.2.0 --no-ff
git tag -a "v1.2.0" -m "v1.2.0"
git checkout develop
git merge release/v1.2.0 --no-ff