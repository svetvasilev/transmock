/***************************************
//   Copyright 2014 - Svetoslav Vasilev

//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//     http://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
*****************************************/

/// -----------------------------------------------------------------------------------------------------------
/// Module      :  MockAdapterBindingCollectionElement.cs
/// Description :  Binding Collection Element class which implements the StandardBindingCollectionElement
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
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
