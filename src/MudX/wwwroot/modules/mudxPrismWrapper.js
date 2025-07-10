
export async function injectCssFromFile(cssPath){let linkTag=document.querySelector('link[data-prism="true"]');if(linkTag){linkTag.href=cssPath;}else{linkTag=document.createElement("link");linkTag.setAttribute("data-prism","true");linkTag.type="text/css";linkTag.rel="stylesheet";linkTag.href=cssPath;document.head.appendChild(linkTag);}}
export async function loadPrism(){return new Promise((resolve,reject)=>{if(document.querySelector("script[data-prism]")){return resolve();}
const script=document.createElement("script");script.setAttribute("data-prism","true");script.type="text/javascript";script.src="./_content/MudX/prism/prism.js";script.onload=()=>resolve();script.onerror=()=>reject(new Error("Failed to load Prism.js"));document.head.appendChild(script);setTimeout(resolve,150);});}
export async function initialize(cssPath){try{await injectCssFromFile(cssPath);await loadPrism();return true;}catch(error){console.error("Initialization failed:",error);return false;}}
export function highlightElementById(elementId){if(!window.Prism){return;}
const element=document.getElementById(elementId);if(!element){console.error(`Element with id'${elementId}'not found.`);return;}
window.Prism.highlightAllUnder(element);}