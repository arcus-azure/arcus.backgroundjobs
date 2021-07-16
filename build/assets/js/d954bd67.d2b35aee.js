(self.webpackChunkarcus_background_jobs=self.webpackChunkarcus_background_jobs||[]).push([[217],{3905:function(e,t,r){"use strict";r.d(t,{Zo:function(){return l},kt:function(){return v}});var n=r(7294);function i(e,t,r){return t in e?Object.defineProperty(e,t,{value:r,enumerable:!0,configurable:!0,writable:!0}):e[t]=r,e}function o(e,t){var r=Object.keys(e);if(Object.getOwnPropertySymbols){var n=Object.getOwnPropertySymbols(e);t&&(n=n.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),r.push.apply(r,n)}return r}function a(e){for(var t=1;t<arguments.length;t++){var r=null!=arguments[t]?arguments[t]:{};t%2?o(Object(r),!0).forEach((function(t){i(e,t,r[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(r)):o(Object(r)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(r,t))}))}return e}function c(e,t){if(null==e)return{};var r,n,i=function(e,t){if(null==e)return{};var r,n,i={},o=Object.keys(e);for(n=0;n<o.length;n++)r=o[n],t.indexOf(r)>=0||(i[r]=e[r]);return i}(e,t);if(Object.getOwnPropertySymbols){var o=Object.getOwnPropertySymbols(e);for(n=0;n<o.length;n++)r=o[n],t.indexOf(r)>=0||Object.prototype.propertyIsEnumerable.call(e,r)&&(i[r]=e[r])}return i}var u=n.createContext({}),s=function(e){var t=n.useContext(u),r=t;return e&&(r="function"==typeof e?e(t):a(a({},t),e)),r},l=function(e){var t=s(e.components);return n.createElement(u.Provider,{value:t},e.children)},p={inlineCode:"code",wrapper:function(e){var t=e.children;return n.createElement(n.Fragment,{},t)}},d=n.forwardRef((function(e,t){var r=e.components,i=e.mdxType,o=e.originalType,u=e.parentName,l=c(e,["components","mdxType","originalType","parentName"]),d=s(r),v=i,y=d["".concat(u,".").concat(v)]||d[v]||p[v]||o;return r?n.createElement(y,a(a({ref:t},l),{},{components:r})):n.createElement(y,a({ref:t},l))}));function v(e,t){var r=arguments,i=t&&t.mdxType;if("string"==typeof e||i){var o=r.length,a=new Array(o);a[0]=d;var c={};for(var u in t)hasOwnProperty.call(t,u)&&(c[u]=t[u]);c.originalType=e,c.mdxType="string"==typeof e?e:i,a[1]=c;for(var s=2;s<o;s++)a[s]=r[s];return n.createElement.apply(null,a)}return n.createElement.apply(null,r)}d.displayName="MDXCreateElement"},9823:function(e,t,r){"use strict";r.r(t),r.d(t,{frontMatter:function(){return c},contentTitle:function(){return u},metadata:function(){return s},toc:function(){return l},default:function(){return d}});var n=r(2122),i=r(9756),o=(r(7294),r(3905)),a=["components"],c={title:"Automatically Invalidate Azure Key Vault Secrets",layout:"default"},u="Automatically Invalidate Azure Key Vault Secrets",s={unversionedId:"features/security/auto-invalidate-secrets",id:"version-0.1/features/security/auto-invalidate-secrets",isDocsHomePage:!1,title:"Automatically Invalidate Azure Key Vault Secrets",description:"The Arcus.BackgroundJobs.KeyVault library provides a background job to automatically invalidate cached Azure Key Vault secrets from an ICachedSecretProvider instance of your choice.",source:"@site/versioned_docs/version-0.1/features/security/auto-invalidate-secrets.md",sourceDirName:"features/security",slug:"/features/security/auto-invalidate-secrets",permalink:"/0.1/features/security/auto-invalidate-secrets",editUrl:"https://github.com/facebook/docusaurus/edit/master/website/versioned_docs/version-0.1/features/security/auto-invalidate-secrets.md",version:"0.1",frontMatter:{title:"Automatically Invalidate Azure Key Vault Secrets",layout:"default"},sidebar:"version-0.1/tutorialSidebar",previous:{title:"Securely Receive CloudEvents",permalink:"/0.1/features/cloudevent/receive-cloudevents-job"},next:{title:"Home",permalink:"/0.1/index"}},l=[{value:"How does it work?",id:"how-does-it-work",children:[]},{value:"Usage",id:"usage",children:[]}],p={toc:l};function d(e){var t=e.components,c=(0,i.Z)(e,a);return(0,o.kt)("wrapper",(0,n.Z)({},p,c,{components:t,mdxType:"MDXLayout"}),(0,o.kt)("h1",{id:"automatically-invalidate-azure-key-vault-secrets"},"Automatically Invalidate Azure Key Vault Secrets"),(0,o.kt)("p",null,"The ",(0,o.kt)("inlineCode",{parentName:"p"},"Arcus.BackgroundJobs.KeyVault")," library provides a background job to automatically invalidate cached Azure Key Vault secrets from an ",(0,o.kt)("inlineCode",{parentName:"p"},"ICachedSecretProvider")," instance of your choice."),(0,o.kt)("h2",{id:"how-does-it-work"},"How does it work?"),(0,o.kt)("a",{href:"https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Farcus-azure%2Farcus.backgroundjobs%2Fmaster%2Fdeploy%2Farm%2Fazure-key-vault-job.json",target:"_blank"},(0,o.kt)("img",{src:"https://azuredeploy.net/deploybutton.png"})),(0,o.kt)("p",null,"This automation works by subscribing on the ",(0,o.kt)("inlineCode",{parentName:"p"},"SecretNewVersionCreated")," event of an Azure Key Vault resource and placing those events on a Azure Service Bus Topic; which we process in our background job."),(0,o.kt)("p",null,(0,o.kt)("img",{alt:"Automatically Invalidate Azure Key Vault Secrets",src:r(2353).Z})),(0,o.kt)("p",null,"To make this automation opperational, following Azure Resources has to be used:"),(0,o.kt)("ul",null,(0,o.kt)("li",{parentName:"ul"},"Azure Key Vault instance"),(0,o.kt)("li",{parentName:"ul"},"Azure Service Bus Topic"),(0,o.kt)("li",{parentName:"ul"},"Azure Event Grid subscription for ",(0,o.kt)("inlineCode",{parentName:"li"},"SecretNewVersionCreated")," events that are sent to the Azure Service Bus Topic")),(0,o.kt)("h2",{id:"usage"},"Usage"),(0,o.kt)("p",null,"Our background job has to be configured in ",(0,o.kt)("inlineCode",{parentName:"p"},"ConfigureServices")," method:"),(0,o.kt)("pre",null,(0,o.kt)("code",{parentName:"pre",className:"language-csharp"},"using Arcus.Security.Core;\nusing Arcus.Security.Core.Caching;\nusing Microsoft.Extensions.DependencyInjection;\n\npublic class Startup\n{\n    public void ConfigureServices(IServiceCollection services)\n    {\n        // An 'ISecretProvider' implementation (see: https://security.arcus-azure.net/) to access the Azure Service Bus Topic resource;\n        //     this will get the 'serviceBusTopicConnectionStringSecretKey' string (configured below) and has to retrieve the connection string for the topic.\n        services.AddSingleton<ISecretProvider>(serviceProvider => ...);\n\n        // An `ICachedSecretProvider` implementation which secret keys will automatically be invalidated.\n        services.AddSingleton<ICachedSecretProvider>(serviceProvider => new CachedSecretProvider(mySecretProvider));\n\n        services.AddAutoInvalidateKeyVaultSecretBackgroundJob(\n            // Prefix of the Azure Service Bus Topic subscription;\n            //    this allows the background jobs to support applications that are running multiple instances, processing the same type of events, without conflicting subscription names.\n            subscriptionNamePrefix: \"MyPrefix\"\n\n            // Connection string secret key to a Azure Service Bus Topic.\n            serviceBusTopicConnectionStringSecretKey: \"MySecretKeyToServiceBusTopicConnectionString\");\n    }\n}\n")),(0,o.kt)("p",null,(0,o.kt)("a",{parentName:"p",href:"/"},"\u2190"," back")))}d.isMDXComponent=!0},2353:function(e,t,r){"use strict";t.Z=r.p+"assets/images/Azure-Key-Vault-Job-4379ac4e4bc2ed9b817b6ad5465b6dc8.png"}}]);