﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Nancy;
using Nancy.ModelBinding;

namespace Neo4jManager.Host.Deployments
{
    public class DeploymentsModule : NancyModule
    {
        public DeploymentsModule(INeo4jDeploymentsPool pool, IMapper mapper) : base("/deployments")
        {
            // Get all deployments
            Get("/", _ => mapper.Map<IEnumerable<Deployment>>(pool.Deployments));

            // Get single deployment
            Get("/{Id}", ctx =>
            {
                string id = ctx.Id.ToString();
                return mapper.Map<Deployment>(pool.Deployments.Single(d => d.Key == id));
            });

            // Create deployment
            Post("/", async (ctx, ct) =>
            {
                var deployment = this.Bind<DeploymentRequest>();
                await Task.Run(() => pool.Create(Neo4jVersions.GetVersions().Single(v => v.Version == deployment.Version), deployment.Id));
                return mapper.Map<Deployment>(pool.Deployments.Single(d => d.Key == deployment.Id));
            });

            // Delete all deployments
            Delete("/all", async (ctx, ct) =>
            {
                await Task.Run(() => pool.DeleteAll());
                return (Response)null;
            });

            // Delete single deployment
            Delete("/{Id}", async (ctx, ct) =>
            {
                string id = ctx.Id.ToString();
                await Task.Run(() => pool.Delete(id));
                return (Response) null;
            });

            
            // Start instance
            Post("/{Id}/start", async (ctx, ct) =>
            {
                string id = ctx.Id.ToString();
                await pool.Deployments[id].Start(ct);
                return (Response) null;
            });

            // Stop instance
            Post("/{Id}/stop", async (ctx, ct) =>
            {
                string id = ctx.Id.ToString();
                await pool.Deployments[id].Stop(ct);
                return (Response)null;
            });

            // Restart instance
            Post("/{Id}/restart", async (ctx, ct) =>
            {
                string id = ctx.Id.ToString();
                await pool.Deployments[id].Restart(ct);
                return (Response)null;
            });

            // Clear instance (delete data)
            Post("/{Id}/clear", async (ctx, ct) =>
            {
                string id = ctx.Id.ToString();
                await pool.Deployments[id].Clear(ct);
                return (Response)null;
            });

            // Backup instance data
            Post("/{Id}/backup", async (ctx, ct) =>
            {
                var backup = this.Bind<BackupRequest>();
                await pool.Deployments[backup.Id].Backup(ct, backup.DestinationPath, backup.StopInstanceBeforeBackup);
                return (Response)null;
            });

            // Restore instance data
            Post("/{Id}/restore", async (ctx, ct) =>
            {
                var restore = this.Bind<RestoreRequest>();
                await pool.Deployments[restore.Id].Restore(ct, restore.SourcePath);
                return (Response)null;
            });

            // Modify instance config
            Post("/{Id}/config", async (ctx, ct) =>
            {
                var config = this.Bind<ConfigureRequest>();
                await Task.Run(() => pool.Deployments[config.Id].Configure(config.ConfigFile, config.Key, config.Value));
                return (Response)null;
            });
        }
    }
}
