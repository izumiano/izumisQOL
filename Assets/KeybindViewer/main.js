const keyboardElement = document.getElementById("keyboard");
const modList = document.getElementById("modList");

function safeDictPush(dict, key, val){
	if(key in dict){
		let keyData = dict[key];
		keyData.push(val);
		dict[key] = keyData;
		return;
	}
	dict[key] = [val];
}

function getModKeybinds(keybinds){
	let keyElements = {};
	for(const row of keyboardElement.children){
		for(const keyEl of row.children){
			const key = keyEl.dataset.key;
			if(key === undefined){
				continue;
			}
			safeDictPush(keyElements, key, keyEl);
		}
	}

	let mods = {};
	for(const key of Object.entries(keybinds)){
		const keyName = key[0];
		const keyList = keyElements[keyName];
		if(keyList === undefined){
			console.log(keyName + " did not have a corresponding key");
			continue;
		}
		
		for(const child of keyList){
			let tooltip = ""
			for(const keyInfo of key[1]){
				tooltip += keyInfo.Module + ": " + keyInfo.PropertyName + "<br>";
				
				safeDictPush(mods, keyInfo.Module, child);
			}
			child.setAttribute("tooltip", tooltip);
		}
	}
	return mods;
}

function displayMods(modKeybinds){
	for(const mod of Object.entries(modKeybinds)) {
		const modName = mod[0];
		const keys = mod[1];
		let ul = document.createElement("ul");
		ul.innerText = modName;
		ul.onclick = () => {
			if(modName === selectedMod){
				selectedMod = null;
				displayModKeybinds(modKeybinds, modName);
				ul.classList.remove("selected");
				return;
			}
			selectedMod = modName;
			
			let obj = {}
			obj[modName] = keys;
			displayModKeybinds(obj);
			
			for(const child of modList.children){
				child.classList.remove("selected");
			}
			
			ul.classList.add("selected");
		}
		ul.onmouseover = () => {
			if(selectedMod == null){
				displayModKeybinds(modKeybinds, modName);
			}
		}
		ul.onmouseleave = () => {
			if(selectedMod === null){
				displayModKeybinds(modKeybinds);
			}
		}
		modList.appendChild(ul);
	}
}

function displayModKeybinds(modKeybinds, hoveringOver = null){
	for(const row of keyboardElement.children){
		for(const keyEl of row.children){
			const key = keyEl.dataset.key;
			if(key === undefined){
				continue;
			}
			keyEl.classList.remove("hasKeybind");
			keyEl.classList.remove("modHovering");
			keyEl.setAttribute("title", "");
		}
	}
	
	for(const mod of Object.entries(modKeybinds)){
		const modName = mod[0];
		const keys = mod[1];
		
		for(const key of keys){
			key.classList.add("hasKeybind");
			key.setAttribute("title", key.getAttribute("tooltip") ?? "");
			
			if(modName === hoveringOver){
				key.classList.add("modHovering");
			}
		}
	}
}

async function main()
{
	const keybindInfo = await (await fetch('http://localhost:32270/izumisQOL/getKeybinds')).json();

	const modKeybinds = getModKeybinds(keybindInfo.Bindings.Keybinds);
	displayModKeybinds(modKeybinds);
	displayMods(modKeybinds);
}

let selectedMod = null;

main();
