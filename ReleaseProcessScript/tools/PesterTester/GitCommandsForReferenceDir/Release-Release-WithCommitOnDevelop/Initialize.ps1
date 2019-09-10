git checkout -b develop --quiet *>$NULL
git commit --allow-empty -m "Develop is now ahead of the ReleaseBranch Root" *>$NULL
git checkout -b release/v1.2.0 --quiet *>$NULL