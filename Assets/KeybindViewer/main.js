const keyboardElement = document.getElementById("keyboard");

function safeDictPush(dict, key, val){
	if(key in dict){
		let keyData = dict[key];
		keyData.push(val);
		dict[key] = keyData;
		return;
	}
	dict[key] = [val];
}

function getKeyData(keybinds){
	let keyData = {};
	for(const key of Object.entries(keybinds)){
		const keyName = key[0];
		const keyList = keys[keyName];
		if(keyList === undefined){
			console.log(keyName + "did not have a corresponding key");
			continue;
		}
		for(const child of keyList){
			child.classList.add("hasKeybind");
			
			let tooltip = ""
			for(const keyInfo of key[1]){
				tooltip += keyInfo.Module + "<br>";
			}
			child.setAttribute("title", tooltip);

			child.onclick = onKeyPress;
			keyData[child.dataset.key] = key[1];
		}
	}
	return keyData;
}

function onKeyPress(){
	const keyButton = this;
	const key = keyButton.dataset.key;
	
	for(const item of keyData[key]){
		console.log(item.Module);
	}
}

async function displayKeybindInfo(){
	const keybindInfo = await (await fetch('http://localhost:32270/izumisQOL/getKeybinds')).json();
	console.log(keybindInfo);

	keys = {};
	for(const row of keyboardElement.children){
		for(const keyEl of row.children){
			const key = keyEl.dataset.key;
			if(key === undefined){
				continue;
			}
			safeDictPush(keys, key, keyEl);
		}
	}
	keyData = getKeyData(keybindInfo.Bindings.Keybinds);
}

displayKeybindInfo();

let keys = {};
let keyData = {}
