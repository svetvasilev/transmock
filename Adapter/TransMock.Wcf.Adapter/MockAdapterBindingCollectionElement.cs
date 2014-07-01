/// -----------------------------------------------------------------------------------------------------------
/// Module      :  WCFMockAdapterBindingCollectionElement.cs
/// Description :  Binding Collection Element class which implements the StandardBindingCollectionElement
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Configuration;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// Initializes a new instance of the WCFMockAdapterBindingCollectionElement class
    /// </summary>
    public class MockAdapterBindingCollectionElement : StandardBindingCollectionElement<MockAdapterBinding,
        MockAdapterBindingElement>
    {
    }
}

