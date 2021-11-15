namespace Mech.Core.ScreenFlow.BaseScreen.Presenter
{
    using Cysharp.Threading.Tasks;
    using Mech.Core.MVP;
    using Mech.Core.ScreenFlow.BaseScreen.Model;
    using Mech.Core.ScreenFlow.BaseScreen.View;
    using Mech.Core.ScreenFlow.Signals;
    using Mech.Services;
    using UnityEngine;
    using Zenject;

    public abstract class BaseScreenPresenter<TView> : IScreenPresenter where TView : IScreenView
    {
        public virtual bool         IsClosePrevious { get; protected set; } = false;
        public         ScreenStatus ScreenStatus    { get; protected set; } = ScreenStatus.Closed;

        public             TView     View;
        protected readonly SignalBus SignalBus;

        public BaseScreenPresenter(SignalBus signalBus) { this.SignalBus = signalBus; }

        #region Implement IUIPresenter

        [Inject] private ILogService logger;

        [Inject]
        public virtual void Initialize() { }

        public async void SetView(IUIView viewInstance)
        {
            this.View = (TView)viewInstance;
            if (this.View.IsReadyToUse)
            {
                this.OnViewReady();
            }
            else
            {
                await UniTask.WaitUntil(() => this.View.IsReadyToUse);
                this.OnViewReady();
            }
        }

        public void SetViewParent(Transform parent)
        {
            if (parent == null)
            {
                this.logger.LogWithColor(parent.name + "is null", Color.green);
                return;
            }

            if (this.View.Equals(null)) return;
            this.View.RectTransform.SetParent(parent);
        }

        public abstract void BindData();

        public virtual void OpenView()
        {
            // Always fill data for screen
            this.BindData();

            if (this.ScreenStatus == ScreenStatus.Opened) return;
            this.ScreenStatus = ScreenStatus.Opened;
            this.View.Open();
            this.SignalBus.Fire(new ScreenShowSignal() { ScreenPresenter = this });
        }

        public virtual void CloseView()
        {
            if (this.ScreenStatus == ScreenStatus.Closed) return;
            this.ScreenStatus = ScreenStatus.Closed;
            this.View.Close();
            this.SignalBus.Fire(new ScreenHideSignal() { ScreenPresenter = this });
            this.Dispose();
        }

        public virtual void HideView()
        {
            if (this.ScreenStatus == ScreenStatus.Hide) return;
            this.ScreenStatus = ScreenStatus.Hide;
            this.View.Hide();
            this.Dispose();
        }
        public virtual void DestroyView()
        {
            if (this.ScreenStatus == ScreenStatus.Destroyed) return;
            this.ScreenStatus = ScreenStatus.Destroyed;
            if (this.View.Equals(null)) return;
            this.Dispose();
            this.View.DestroySelf();
        }

        public virtual void OnOverlap()      { }
        public         int  ViewSiblingIndex { get => this.View.RectTransform.GetSiblingIndex(); set => this.View.RectTransform.SetSiblingIndex(value); }

        #endregion


        protected virtual void OnViewReady()     { this.View.ViewDidDestroy += this.OnViewDestroyed; }
        protected virtual void OnViewDestroyed() { this.SignalBus.Fire(new ScreenSelfDestroyedSignal() { ScreenPresenter = this }); }

        public virtual void Dispose() { }
    }

    public abstract class BaseScreenPresenter<TView, TModel> : BaseScreenPresenter<TView>, IScreenPresenter<TModel> where TView : IScreenView
    {
        protected readonly ILogService Logger;
        protected          TModel      Model;
        protected BaseScreenPresenter(SignalBus signalBus, ILogService logger) : base(signalBus) { this.Logger = logger; }

        public override void OpenView()
        {
            base.OpenView();
            if (this.Model != null)
            {
                this.BindData(this.Model);
            }
            else
            {
                this.Logger.Warning($"{this.GetType().Name} don't have Model!!!");
            }
        }
        public virtual void OpenView(TModel model)
        {
            if (model != null)
            {
                this.Model = model;
            }

            this.OpenView();
        }

        public sealed override void BindData() { }

        public abstract void BindData(TModel screenModel);
    }
}