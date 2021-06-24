git checkout -b "support/v2.28" *>$NULL
git commit -m "Commit on Support Branch" --allow-empty *>$NULL
git tag -a "v2.28.0" -m "v2.28.0" *>$NULL

git checkout -b "support/v2.29" *>$NULL

git checkout -b "hotfix/v2.29.1" *>$NULL
git commit -m "Feature-A" --allow-empty *>$NULL

git checkout -b "prelease/v2.29.1-alpha.1" *>$NULL
git commit -m "Update metadata to version '2.29.1-alpha.1'." --allow-empty *>$NULL
git tag -a "v2.29.1-alpha.1" -m "v2.29.1-alpha.1" *>$NULL

git checkout "hotfix/v2.29.1" *>$NULL
git merge "prelease/v2.29.1-alpha.1" --no-ff *>$NULL

git checkout "hotfix/v2.29.1" *>$NULL
git commit -m "Feature-B" --allow-empty *>$NULL

git checkout -b "prelease/v2.29.1-alpha.2" *>$NULL
git commit -m "Update metadata to version '2.29.1-alpha.2'." --allow-empty *>$NULL
git tag -a "v2.29.1-alpha.2" -m "v2.29.1-alpha.2" *>$NULL

git checkout "hotfix/v2.29.1" *>$NULL
git merge "prelease/v2.29.1-alpha.2" --no-ff *>$NULL
