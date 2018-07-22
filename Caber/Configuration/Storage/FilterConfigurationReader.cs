using System;
using Caber.Configuration.Storage.Validation;
using Caber.FileSystem;
using Caber.FileSystem.Filters;
using Caber.Util;

namespace Caber.Configuration.Storage
{
    public class FilterConfigurationReader
    {
        private readonly IErrorReceiver errors;
        private readonly FileSystemCasing casing;

        public FilterConfigurationReader(IErrorReceiver errors, FileSystemCasing casing)
        {
            this.errors = errors;
            this.casing = casing;
        }

        public RelativePathMatcher Read(MatchElement filter)
        {
            var matcherCount = 0;
            var matcher = new RelativePathMatcher(filter.Rule);
            if (filter.Regex != null)
            {
                matcherCount++;
                matcher = TryReadRegex(filter);
            }
            if (filter.Glob != null)
            {
                matcherCount++;
                matcher = TryReadGlob(filter);
            }
            if (filter.Extension != null)
            {
                matcherCount++;
                matcher = TryReadExtension(filter);
            }
            if (matcherCount > 1)
            {
                errors.Record(new MultipleMatchersSpecified()).SetLocation(filter.ElementInformation);
            }
            return matcher;
        }

        private RelativePathMatcher TryReadRegex(MatchElement filter)
        {
            try
            {
                var regex = FileSystemRegexHelpers.CreateRegex(filter.Regex, casing);
                return new RelativePathMatcher(regex, filter.Rule, $"Regex({filter.Regex})");
            }
            catch (Exception ex)
            {
                errors.Record(new InvalidFilterRegex(filter.Regex, ex.Message)).SetLocation(filter.ElementInformation);
                return default;
            }
        }

        private RelativePathMatcher TryReadGlob(MatchElement filter)
        {
            try
            {
                var regex = new GlobToRegexCompiler().CompileRegex(filter.Glob, casing);
                return new RelativePathMatcher(regex, filter.Rule, $"Glob({filter.Glob})");
            }
            catch (GlobFormatException ex)
            {
                errors.Record(new InvalidFilterGlob(filter.Glob, ex.Message, ex.ErrorPosition)).SetLocation(filter.ElementInformation);
                return default;
            }
        }

        private RelativePathMatcher TryReadExtension(MatchElement filter)
        {
            try
            {
                var regex = new FileExtensionToRegexCompiler().CompileRegex(filter.Extension, casing);
                return new RelativePathMatcher(regex, filter.Rule, $"Extension({filter.Extension})");
            }
            catch (FileExtensionFormatException ex)
            {
                errors.Record(new InvalidFilterFileExtension(filter.Extension, ex.Message)).SetLocation(filter.ElementInformation);
                return default;
            }
        }
    }
}
