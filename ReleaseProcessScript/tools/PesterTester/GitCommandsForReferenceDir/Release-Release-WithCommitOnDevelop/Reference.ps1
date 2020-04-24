git checkout master *>$NULL
git merge release/v1.2.0 --no-ff *>$NULL
git tag -a "v1.2.0" -m "v1.2.0" *>$NULL
git checkout develop *>$NULL
git merge release/v1.2.0 --no-ff *>$NULL