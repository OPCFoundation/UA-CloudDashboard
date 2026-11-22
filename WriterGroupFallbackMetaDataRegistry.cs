
namespace Opc.Ua.Cloud.Dashboard
{
    using Opc.Ua;
    using Opc.Ua.PubSub.MetaData;
    using System;

    /// <summary>
    /// Metadata registry that falls back to WriterGroupId 0 when no metadata is registered for the
    /// specific WriterGroupId of a dataset message. UADP metadata discovery responses typically
    /// don't carry a group header, while the corresponding data messages do.
    /// </summary>
    public sealed class WriterGroupFallbackMetaDataRegistry : IDataSetMetaDataRegistry
    {
        private readonly IDataSetMetaDataRegistry _inner;

        public WriterGroupFallbackMetaDataRegistry(IDataSetMetaDataRegistry inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public ArrayOf<DataSetMetaDataKey> Keys => _inner.Keys;

        public event EventHandler<DataSetMetaDataChangedEventArgs> MetaDataChanged
        {
            add => _inner.MetaDataChanged += value;
            remove => _inner.MetaDataChanged -= value;
        }

        public MetaDataMatchResult TryGet(in DataSetMetaDataKey key, out DataSetMetaDataType metaData)
        {
            MetaDataMatchResult result = _inner.TryGet(in key, out metaData);
            if ((result == MetaDataMatchResult.NotFound) && (key.WriterGroupId != 0))
            {
                DataSetMetaDataKey fallbackKey = new DataSetMetaDataKey(key.PublisherId, 0, key.DataSetWriterId, key.DataSetClassId, key.MajorVersion);
                result = _inner.TryGet(in fallbackKey, out metaData);
            }

            return result;
        }

        public void Register(in DataSetMetaDataKey key, DataSetMetaDataType metaData)
        {
            _inner.Register(in key, metaData);
        }

        public void Remove(in DataSetMetaDataKey key)
        {
            _inner.Remove(in key);
        }
    }
}
