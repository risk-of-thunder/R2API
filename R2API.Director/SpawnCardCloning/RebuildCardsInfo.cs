using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace R2API.SpawnCardCloning;
public struct RebuildCardsInfo
{
    public ClassicStageInfo classicStageInfo;
    public DirectorCardCategorySelection forcedMonsterCategory;
    public DirectorCardCategorySelection forcedInteractableCategory;
    public DccsPool dccsPool;
    public DccsPool.Category[] categories;
    public DccsPool.PoolEntry poolEntry;
    public DirectorCardCategorySelection directorCardCategorySelection;
    public DccsPool.Category dccsPoolCategory;
    public DirectorCardCategorySelection.Category category;
    public DirectorCard directorCard;
    public SpawnCard spawnCard;
    public object spawnCardClone;
    public DirectorCard clonedDirectorCard;
    public SpawnCard clonedSpawnCard;
}
