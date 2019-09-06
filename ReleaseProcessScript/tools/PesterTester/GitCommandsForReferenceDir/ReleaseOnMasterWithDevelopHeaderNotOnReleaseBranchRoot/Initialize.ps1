git checkout -b develop --quiet
git checkout -b prerelease/v1.2.0-rc.1 --quiet
git checkout -b release/v1.2.0 --quiet
git commit --allow-empty -m "Develop is now ahead of the ReleaseBranch Root"
git checkout release/v1.2.0 --quiet