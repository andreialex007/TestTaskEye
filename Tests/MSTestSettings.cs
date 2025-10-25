// Disable parallel test execution to avoid file access conflicts
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]
