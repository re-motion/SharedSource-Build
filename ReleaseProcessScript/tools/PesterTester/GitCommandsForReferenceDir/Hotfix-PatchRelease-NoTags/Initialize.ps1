git checkout -b "support/v2.28" *>$NULL
git commit -m "Commit on Support Branch" --allow-empty *>$NULL

git checkout -b "hotfix/v2.28.1" *>$NULL
git commit -m "Feature-A" --allow-empty *>$NULL
git commit -m "Feature-B" --allow-empty *>$NULL
