git checkout -b release/v1.2.0 *>$NULL
git commit -m "Commit on release branch" --allow-empty *>$NULL
git checkout master --quiet *>$NULL
git merge release/v1.2.0 --no-ff *>$NULL
git tag -a "v1.2.0" -m "v1.2.0" *>$NULL
git checkout develop *>$NULL
git merge release/v1.2.0 --no-ff *>$NULL