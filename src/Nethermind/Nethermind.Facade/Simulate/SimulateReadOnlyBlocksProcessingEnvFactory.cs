// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Blockchain;
using Nethermind.Blockchain.Blocks;
using Nethermind.Blockchain.Headers;
using Nethermind.Blockchain.Synchronization;
using Nethermind.Core.Specs;
using Nethermind.Db;
using Nethermind.Db.Blooms;
using Nethermind.Logging;
using Nethermind.State;
using Nethermind.State.Repositories;
using Nethermind.Trie.Pruning;

namespace Nethermind.Facade.Simulate;

public class SimulateReadOnlyBlocksProcessingEnvFactory(
    IWorldStateManager worldStateManager,
    IReadOnlyBlockTree baseBlockTree,
    IDbProvider dbProvider,
    ISpecProvider specProvider,
    ILogManager? logManager = null)
{
    public SimulateReadOnlyBlocksProcessingEnv Create(bool validate)
    {
        IReadOnlyDbProvider editableDbProvider = new ReadOnlyDbProvider(dbProvider, true);
        OverlayTrieStore overlayTrieStore = new(editableDbProvider.StateDb, worldStateManager.TrieStore, logManager);
        OverlayWorldStateManager overlayWorldStateManager = new(editableDbProvider, overlayTrieStore, logManager);
        IWorldState worldState = overlayWorldStateManager.GlobalWorldState;
        BlockTree tempBlockTree = CreateTempBlockTree(editableDbProvider, specProvider, logManager, editableDbProvider);

        return new SimulateReadOnlyBlocksProcessingEnv(
            worldState,
            baseBlockTree,
            editableDbProvider,
            tempBlockTree,
            specProvider,
            logManager,
            validate);
    }

    private static BlockTree CreateTempBlockTree(IReadOnlyDbProvider readOnlyDbProvider, ISpecProvider? specProvider, ILogManager? logManager, IReadOnlyDbProvider editableDbProvider)
    {
        IBlockStore mainblockStore = new BlockStore(editableDbProvider.BlocksDb);
        IHeaderStore mainHeaderStore = new HeaderStore(editableDbProvider.HeadersDb, editableDbProvider.BlockNumbersDb);
        SimulateDictionaryHeaderStore tmpHeaderStore = new(mainHeaderStore);
        const int badBlocksStored = 1;

        SimulateDictionaryBlockStore tmpBlockStore = new(mainblockStore);
        IBadBlockStore badBlockStore = new BadBlockStore(editableDbProvider.BadBlocksDb, badBlocksStored);

        return new(tmpBlockStore,
            tmpHeaderStore,
            editableDbProvider.BlockInfosDb,
            editableDbProvider.MetadataDb,
            badBlockStore,
            new ChainLevelInfoRepository(readOnlyDbProvider.BlockInfosDb),
            specProvider,
            NullBloomStorage.Instance,
            new SyncConfig(),
            logManager);
    }
}
