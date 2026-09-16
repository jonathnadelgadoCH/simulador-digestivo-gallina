using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigestiveSimulator.Presentation
{
    public interface IAnatomyView
    {
        Transform Root { get; }
        IReadOnlyDictionary<string, OrganView> Views { get; }
        void SetVisibility(AnatomyVisibility mode);
        void SetActive(bool active);
        bool TryGetView(string organId, out OrganView view);
        void Clear();
    }
}
