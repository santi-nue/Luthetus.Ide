﻿using Fluxor;
using Luthetus.Common.RazorLib.Reflectives.Models;
using Luthetus.Common.RazorLib.Reflectives.States;
using Luthetus.Common.RazorLib.Icons.Displays.Codicon;
using Luthetus.Common.RazorLib.Keys.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Luthetus.Common.Tests.Basis.Reflectives.States;

/// <summary>
/// <see cref="ReflectiveState.Reducer"/>
/// </summary>
public class ReflectiveStateReducerTests
{
    /// <summary>
    /// <see cref="ReflectiveState.Reducer.ReduceRegisterAction(ReflectiveState, ReflectiveState.RegisterAction)"/>
    /// </summary>
    [Fact]
    public void ReduceRegisterAction()
    {
        InitializeReflectiveStateReducerTests(
            out var dispatcher,
            out var reflectiveStateWrap,
            out var model,
            out _);

        var insertionIndex = 0;

        var registerAction = new ReflectiveState.RegisterAction(model, insertionIndex);

        Assert.DoesNotContain(reflectiveStateWrap.Value.ReflectiveModelBag,
            x => x == model);

        dispatcher.Dispatch(registerAction);

        Assert.Contains(reflectiveStateWrap.Value.ReflectiveModelBag,
            x => x == model);
    }

    /// <summary>
    /// <see cref="ReflectiveState.Reducer.ReduceWithAction(ReflectiveState, ReflectiveState.WithAction)"/>
    /// </summary>
    [Fact]
    public void ReduceWithAction()
    {
        InitializeReflectiveStateReducerTests(
            out var dispatcher,
            out var reflectiveStateWrap,
            out var model,
            out var componentTypeBag);

        var insertionIndex = 0;

        // Setup: Register a node
        {
            var registerAction = new ReflectiveState.RegisterAction(model, insertionIndex);
            dispatcher.Dispatch(registerAction);
        }

        var type = componentTypeBag.Last();

        model = reflectiveStateWrap.Value.ReflectiveModelBag.Single(
            x => x.Key == model.Key);

        Assert.NotEqual(type.GUID, model.ChosenTypeGuid);

        var withAction = new ReflectiveState.WithAction(
            model.Key,
            inState =>
            {
                return inState with
                {
                    ChosenTypeGuid = type.GUID
                };
            });

        dispatcher.Dispatch(withAction);

        model = reflectiveStateWrap.Value.ReflectiveModelBag.Single(
            x => x.Key == model.Key);

        Assert.Equal(type.GUID, model.ChosenTypeGuid);
    }

    /// <summary>
    /// <see cref="ReflectiveState.Reducer.ReduceDisposeAction(ReflectiveState, ReflectiveState.DisposeAction)"/>
    /// </summary>
    [Fact]
    public void ReduceDisposeAction()
    {
        InitializeReflectiveStateReducerTests(
            out var dispatcher,
            out var reflectiveStateWrap,
            out var model,
            out _);

        var insertionIndex = 0;

        // Setup: Register a node
        {
            var registerAction = new ReflectiveState.RegisterAction(model, insertionIndex);
            dispatcher.Dispatch(registerAction);
        }

        var disposeAction = new ReflectiveState.DisposeAction(model.Key);

        Assert.Contains(reflectiveStateWrap.Value.ReflectiveModelBag,
            x => x == model);

        dispatcher.Dispatch(disposeAction);

        Assert.DoesNotContain(reflectiveStateWrap.Value.ReflectiveModelBag,
            x => x == model);
    }

    private void InitializeReflectiveStateReducerTests(
        out IDispatcher dispatcher,
        out IState<ReflectiveState> reflectiveStateWrap,
        out ReflectiveModel model,
        out List<Type> componentTypeBag)
    {
        var services = new ServiceCollection()
            .AddFluxor(options => options.ScanAssemblies(typeof(ReflectiveState).Assembly));

        var serviceProvider = services.BuildServiceProvider();

        var store = serviceProvider.GetRequiredService<IStore>();
        store.InitializeAsync().Wait();

        reflectiveStateWrap = serviceProvider.GetRequiredService<IState<ReflectiveState>>();

        dispatcher = serviceProvider.GetRequiredService<IDispatcher>();

        componentTypeBag = new List<Type>
        {
            typeof(IconArrowDown),
            typeof(IconArrowLeft),
            typeof(IconArrowRight),
            typeof(IconArrowUp),
        };

        model = new ReflectiveModel(
            Key<ReflectiveModel>.NewKey(),
            componentTypeBag,
            Guid.Empty,
            Guid.Empty,
            Array.Empty<PropertyInfo>(),
            new(),
            dispatcher);
    }
}