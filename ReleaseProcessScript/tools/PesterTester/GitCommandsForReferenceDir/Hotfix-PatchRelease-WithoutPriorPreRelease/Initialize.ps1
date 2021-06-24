git checkout -b "support/v2.27" *>$NULL
git commit -m "Commit on Support Branch" --allow-empty *>$NULL

# v2.27.1
git checkout -b "hotfix/v2.27.1" *>$NULL
git commit -m "Feature-A1" --allow-empty *>$NULL
git commit -m "Feature-B1" --allow-empty *>$NULL

git checkout "support/v2.27" *>$NULL
git merge --no-ff "hotfix/v2.27.1" *>$NULL
git tag -a "v2.27.1" -m "v2.27.1" *>$NULL

# v2.27.2
git checkout -b "hotfix/v2.27.2" *>$NULL
git commit -m "Feature-A2" --allow-empty *>$NULL
git commit -m "Feature-B2" --allow-empty *>$NULL

git checkout "support/v2.27" *>$NULL
git merge --no-ff "hotfix/v2.27.2" *>$NULL
git tag -a "v2.27.2" -m "v2.27.2" *>$NULL

# v2.27.3
git checkout -b "hotfix/v2.27.3" *>$NULL
git commit -m "Feature-A3" --allow-empty *>$NULL
git commit -m "Feature-B3" --allow-empty *>$NULL

git checkout "support/v2.27" *>$NULL
git merge --no-ff "hotfix/v2.27.3" *>$NULL
git tag -a "v2.27.3" -m "v2.27.3" *>$NULL

# v2.27.4 (to be released)
git checkout -b "hotfix/v2.27.4" *>$NULL
git commit -m "Feature-A4" --allow-empty *>$NULL
git commit -m "Feature-B4" --allow-empty *>$NULL

