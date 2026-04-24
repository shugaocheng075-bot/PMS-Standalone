const fs = require('fs');
const path = 'c:/Users/Administrator/PMS-Standalone/pms-web/src/views/login/LoginView.vue';
let text = fs.readFileSync(path, 'utf8');

// The block to remove is:
/*
            <el-form-item prop="organizationCode">
              <el-select v-model="form.organizationCode" filterable placeholder="机构代码" class="apple-input">
                <el-option v-for="option in organizationOptions" :key="option.value" :label="option.label" :value="option.value" />
              </el-select>
            </el-form-item>
*/

const formItemRegex = /<el-form-item prop="organizationCode">[\s\S]*?<\/el-form-item>/;
text = text.replace(formItemRegex, '');

text = text.replace(/organizationCode:\s*\[[\s\S]*?\]\,/m, '');
text = text.replace(/organizationCode:\s*'',/m, '');

const optionsRegex = /const organizationOptions = \[[\s\S]*?\]/;
text = text.replace(optionsRegex, '');

const onMountedRegex = /onMounted\(\(\) => \{[\s\S]*?refreshCaptcha\(\)[\s\S]*?const savedOrg = localStorage\.getItem\(ORGANIZATION_STORAGE_KEY\)[\s\S]*?if \(!form\.organizationCode\) \{[\s\S]*?\}[\s\S]*?\}\)/;
text = text.replace(onMountedRegex, 'onMounted(() => {\n  refreshCaptcha()\n})');

text = text.replace(/const ORGANIZATION_STORAGE_KEY = 'pms-login-organization'/g, '');
text = text.replace(/localStorage\.setItem\(ORGANIZATION_STORAGE_KEY, form\.organizationCode\)/g, '');

fs.writeFileSync(path, text, 'utf8');
console.log('Done!');
