git checkout master
git merge release/v1.2.0 --no-ff
git tag -a "v1.2.0" -m "v1.2.0"
git checkout develop
git merge release/v1.2.0 --no-ff