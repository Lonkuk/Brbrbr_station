using Content.Client.UserInterface.Controls;
using Content.Shared.Popups;
using Content.Shared._LostParadise.RCDFAP;
using Content.Shared._LostParadise.RCDFAP.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client._LostParadise.RCDFAP;

[GenerateTypedNameReferences]
public sealed partial class RCDFAPMenu : RadialMenu
{
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly SpriteSystem _spriteSystem;
    private readonly SharedPopupSystem _popup;

    public event Action<ProtoId<RCDFAPPrototype>>? SendRCDFAPSystemMessageAction;

    private EntityUid _owner;

    public RCDFAPMenu(EntityUid owner, RCDFAPMenuBoundUserInterface bui)
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        _spriteSystem = _entManager.System<SpriteSystem>();
        _popup = _entManager.System<SharedPopupSystem>();

        _owner = owner;

        // Find the main radial container
        var main = FindControl<RadialContainer>("Main");

        if (main == null)
            return;

        // Populate secondary radial containers
        if (!_entManager.TryGetComponent<RCDFAPComponent>(owner, out var rcdfap))
            return;

        foreach (var protoId in rcdfap.AvailablePrototypes)
        {
            if (!_protoManager.TryIndex(protoId, out var proto))
                continue;

            if (proto.Mode == RcdfapMode.Invalid)
                continue;

            var parent = FindControl<RadialContainer>(proto.Category);

            if (parent == null)
                continue;

            var tooltip = Loc.GetString(proto.SetName);

            tooltip = OopsConcat(char.ToUpper(tooltip[0]).ToString(), tooltip.Remove(0, 1));

            var button = new RCDFAPMenuButton()
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64f, 64f),
                ToolTip = tooltip,
                ProtoId = protoId,
            };

            if (proto.Sprite != null)
            {
                var tex = new TextureRect()
                {
                    VerticalAlignment = VAlignment.Center,
                    HorizontalAlignment = HAlignment.Center,
                    Texture = _spriteSystem.Frame0(proto.Sprite),
                    TextureScale = new Vector2(2f, 2f),
                };

                button.AddChild(tex);
            }

            parent.AddChild(button);

            // Ensure that the button that transitions the menu to the associated category layer
            // is visible in the main radial container (as these all start with Visible = false)
            foreach (var child in main.Children)
            {
                var castChild = child as RadialMenuTextureButton;

                if (castChild is not RadialMenuTextureButton)
                    continue;

                if (castChild.TargetLayer == proto.Category)
                {
                    castChild.Visible = true;
                    break;
                }
            }
        }

        // Set up menu actions
        foreach (var child in Children)
            AddRCDFAPMenuButtonOnClickActions(child);

        OnChildAdded += AddRCDFAPMenuButtonOnClickActions;

        SendRCDFAPSystemMessageAction += bui.SendRCDFAPSystemMessage;
    }

    private static string OopsConcat(string a, string b)
    {
        // This exists to prevent Roslyn being clever and compiling something that fails sandbox checks.
        return a + b;
    }

    private void AddRCDFAPMenuButtonOnClickActions(Control control)
    {
        var radialContainer = control as RadialContainer;

        if (radialContainer == null)
            return;

        foreach (var child in radialContainer.Children)
        {
            var castChild = child as RCDFAPMenuButton;

            if (castChild == null)
                continue;

            castChild.OnButtonUp += _ =>
            {
                SendRCDFAPSystemMessageAction?.Invoke(castChild.ProtoId);

                if (_playerManager.LocalSession?.AttachedEntity != null &&
                    _protoManager.TryIndex(castChild.ProtoId, out var proto))
                {
                    var msg = Loc.GetString("rcdfap-component-change-mode", ("mode", Loc.GetString(proto.SetName)));

                    if (proto.Mode == RcdfapMode.ConstructObject)
                    {
                        var name = Loc.GetString(proto.SetName);

                        if (proto.Prototype != null &&
                            _protoManager.TryIndex(proto.Prototype, out var entProto))
                            name = entProto.Name;

                        msg = Loc.GetString("rcdfap-component-change-build-mode", ("name", name));
                    }

                    // Popup message
                    _popup.PopupClient(msg, _owner, _playerManager.LocalSession.AttachedEntity);
                }

                Close();
            };
        }
    }
}

public sealed class RCDFAPMenuButton : RadialMenuTextureButton
{
    public ProtoId<RCDFAPPrototype> ProtoId { get; set; }

    public RCDFAPMenuButton()
    {

    }
}
