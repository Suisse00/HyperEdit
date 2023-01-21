namespace HyperEdit.Source.View
{
    public class ViewOptionalOptions
    {
        /// <summary>
        /// If provided, will ensure only one windows exists at all time.
        /// </summary>
        public string UniqueId;

        public int? Width;
        public int? Height;

        public bool? SavePosition;

        public ViewOptionalOptions() { }

        public ViewOptionalOptions(ViewOptionalOptions source)
        {
            UniqueId = source.UniqueId;

            Width = source.Width;
            Height = source.Height;

            SavePosition = source.SavePosition; 
        }

        public static ViewOptionalOptions Merge(params ViewOptionalOptions[] viewOptionalOptionsArray)
        {
            var mergedViewOptionalOptions = new ViewOptionalOptions();

            foreach(var viewOptionalOptions in viewOptionalOptionsArray)
            {
                if(viewOptionalOptions == null)
                {
                    continue;
                }

                if(!string.IsNullOrEmpty(viewOptionalOptions.UniqueId))
                {
                    mergedViewOptionalOptions.UniqueId = viewOptionalOptions.UniqueId;
                }

                if(viewOptionalOptions.Width.HasValue)
                {
                    mergedViewOptionalOptions.Width = viewOptionalOptions.Width;
                }

                if (viewOptionalOptions.Height.HasValue)
                {
                    mergedViewOptionalOptions.Height = viewOptionalOptions.Height;
                }

                if (viewOptionalOptions.SavePosition.HasValue)
                {
                    mergedViewOptionalOptions.SavePosition = viewOptionalOptions.SavePosition;
                }
            }

            return mergedViewOptionalOptions;
        }
    }
}
