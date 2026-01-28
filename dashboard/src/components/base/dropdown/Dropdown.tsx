import { Dropdown, DropDownProps } from 'antd';

const OllamaFlowDropdown = (props: DropDownProps) => {
  const { children, ...rest } = props;
  return <Dropdown {...rest}>{children}</Dropdown>;
};

export default OllamaFlowDropdown;
