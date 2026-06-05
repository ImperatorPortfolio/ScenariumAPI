using System;

namespace ScenariumAPI
{
    public class ScenariumHudService
    {
        readonly ScenariumSaveData _data;
        readonly Action<string> _addEvent;
        readonly Action _save;

        ScenariumHUD _hud;

        public const string HudVersion = "0.7.3";

        public ScenariumHudService(ScenariumSaveData data, Action<string> addEvent, Action save)
        {
            _data = data;
            _addEvent = addEvent;
            _save = save;
        }

        public ScenariumHUD Hud
        {
            get { return _hud; }
        }

        public void Create()
        {
            EnsureHud();
            _hud.Create();
        }

        public void Open()
        {
            EnsureHud();
            _hud.Open();
        }

        public void Close()
        {
            if (_hud != null)
                _hud.Close();
        }

        public void Refresh(bool force)
        {
            if (_hud != null)
                _hud.Refresh(force);
        }

        public void SetViewModel(ScenariumViewModel viewModel)
        {
            EnsureHud();
            _hud.SetViewModel(viewModel);
        }

        public void CloseAndDispose()
        {
            if (_hud != null)
                _hud.CloseAndDispose();

            _hud = null;
        }

        void EnsureHud()
        {
            if (_hud == null)
                _hud = new ScenariumHUD(_data, _addEvent, _save);
        }
    }
}
