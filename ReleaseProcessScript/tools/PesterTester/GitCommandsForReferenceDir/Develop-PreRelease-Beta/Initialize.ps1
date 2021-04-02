git checkout -b "develop" *>$NULL
git commit -m "Commit on develop" --allow-empty *>$NULL

git commit -m "Feature-A" --allow-empty *>$NULL

git checkout -b "prelease/v2.28.1-alpha.1" *>$NULL
git commit -m "Update metadata to version '2.28.1-alpha.1'." --allow-empty *>$NULL
git tag -a "v2.28.1-alpha.1" -m "v2.28.1-alpha.1" *>$NULL

git checkout "develop" *>$NULL
git merge "prelease/v2.28.1-alpha.1" --no-ff *>$NULL

git commit -m "Feature-B" --allow-empty *>$NULL

git checkout -b "prelease/v2.28.1-beta.1" *>$NULL
git commit -m "Update metadata to version '2.28.1-beta.1'." --allow-empty *>$NULL
git tag -a "v2.28.1-beta.1" -m "v2.28.1-beta.1" *>$NULL

git checkout "develop" *>$NULL
git merge "prelease/v2.28.1-beta.1" --no-ff *>$NULL
