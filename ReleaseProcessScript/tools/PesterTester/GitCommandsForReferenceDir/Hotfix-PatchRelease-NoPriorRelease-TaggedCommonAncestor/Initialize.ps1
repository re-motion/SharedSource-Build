git checkout -b "support/v2.28" *>$NULL
git commit -m "Commit on Support Branch" --allow-empty *>$NULL
git tag -a "v2.28.0" -m "v2.28.0" *>$NULL

git checkout -b "support/v2.29" *>$NULL

git checkout -b "hotfix/v2.29.0" *>$NULL
git commit -m "Feature-A" --allow-empty *>$NULL
git commit -m "Feature-B" --allow-empty *>$NULL
git commit -m "Feature-C" --allow-empty *>$NULL
