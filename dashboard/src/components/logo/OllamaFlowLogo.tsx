import React from "react";
import OllamaFlowFlex from "../base/flex/Flex";
import OllamaFlowText from "../base/typograpghy/Text";

const OllamaFlowLogo = () => {
  return (
    <OllamaFlowFlex align="center" gap={7} className="fade-in">
      <img src="/images/ollama-flow-icon.png" alt="OllamaFlow" height={40} />
      <OllamaFlowText fontSize={20} weight={600}>
        OllamaFlow
      </OllamaFlowText>
    </OllamaFlowFlex>
  );
};

export default OllamaFlowLogo;
