using System;
using System.Collections.Generic;

namespace SecondDimension.Core
{
    public readonly struct Result<T>
    {
        private readonly T _value;

        private Result(T value, IReadOnlyList<string> errors)
        {
            _value = value;
            Errors = errors ?? Array.Empty<string>();
        }

        public bool IsSuccess => Errors.Count == 0;
        public IReadOnlyList<string> Errors { get; }

        public T Value => IsSuccess
            ? _value
            : throw new InvalidOperationException("A failed result has no value.");

        public static Result<T> Success(T value) => new Result<T>(value, Array.Empty<string>());

        public static Result<T> Failure(params string[] errors) =>
            new Result<T>(default, errors ?? new[] { "Unknown error." });
    }
}

